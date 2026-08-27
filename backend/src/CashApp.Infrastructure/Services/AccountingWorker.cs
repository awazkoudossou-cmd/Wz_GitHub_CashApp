using System.Text.Json;
using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CashApp.Infrastructure.Services;

// Surveille AccountingGenerationQueue toutes les X secondes et traite les demandes PENDING.
// Ne génère jamais d'écritures directement depuis la clôture de caisse : tout passe par la Queue.
public class AccountingWorker : BackgroundService
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccountingWorker> _logger;

    public AccountingWorker(IServiceScopeFactory scopeFactory, ILogger<AccountingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var engine = scope.ServiceProvider.GetRequiredService<IAccountingGenerationEngineService>();
                var audit = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
                var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

                await ProcessPendingAsync(db, engine, audit, clock, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "AccountingWorker : échec du cycle de traitement.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { /* arrêt demandé */ }
        }
    }

    // Traite toutes les demandes PENDING trouvées, par priorité puis ancienneté.
    public static async Task ProcessPendingAsync(
        IAppDbContext db, IAccountingGenerationEngineService engine, IAuditLogger audit, IDateTimeProvider clock,
        CancellationToken ct)
    {
        var pendingItems = await db.AccountingGenerationQueues
            .Where(q => q.Status == QueueStatus.PENDING)
            .OrderByDescending(q => q.Priority).ThenBy(q => q.CreatedDate)
            .ToListAsync(ct);

        foreach (var item in pendingItems)
        {
            if (ct.IsCancellationRequested) break;
            await ProcessOneAsync(item, db, engine, audit, clock, ct);
        }
    }

    // Traite une seule demande : Pending -> Processing -> (Completed | Failed). Extrait en méthode
    // publique statique pour être testable directement, sans conteneur DI ni hébergement complet.
    public static async Task ProcessOneAsync(
        AccountingGenerationQueue item, IAppDbContext db, IAccountingGenerationEngineService engine,
        IAuditLogger audit, IDateTimeProvider clock, CancellationToken ct)
    {
        item.Status = QueueStatus.PROCESSING;
        item.StartedDate = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            var registerIds = ParseRegisterIds(item.CashRegisterIdsJson);
            var dto = new GenerateAccountingEntriesDto(item.StartDate, item.EndDate, registerIds);
            var result = await engine.GenerateAsync(dto, ct, item.RequestedBy);

            item.Status = QueueStatus.COMPLETED;
            item.CompletedDate = clock.UtcNow;
            item.ResultGenerationId = result.Id;

            await audit.LogAsync(AuditAction.COMPLETE, nameof(AccountingGenerationQueue), item.Id,
                $"Génération en file #{item.Id} terminée -> batch {result.Reference} ({result.Entries.Count} écriture(s))", ct: ct);
        }
        catch (Exception ex)
        {
            item.Status = QueueStatus.FAILED;
            item.CompletedDate = clock.UtcNow;
            item.Remarks = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;

            await audit.LogAsync(AuditAction.UPDATE, nameof(AccountingGenerationQueue), item.Id,
                $"Génération en file #{item.Id} échouée : {item.Remarks}", ct: ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static List<int>? ParseRegisterIds(string? json)
        => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<int>>(json);
}
