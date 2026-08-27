using System.Text.Json;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public class AccountingQueueService : IAccountingQueueService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _audit;

    public AccountingQueueService(IAppDbContext db, IDateTimeProvider clock, ICurrentUserService currentUser, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<AccountingQueueItemDto> EnqueueManualAsync(EnqueueManualGenerationDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");

        if (dto.StartDate > dto.EndDate)
            throw new BusinessRuleException("INVALID_DATE_RANGE", "La date de début doit être antérieure ou égale à la date de fin.");

        var settings = await _db.AccountingSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings is null || !settings.IsConfigured)
            throw new BusinessRuleException("ACCOUNTING_NOT_CONFIGURED", "Le moteur comptable n'est pas configuré (voir Paramètres comptables).");

        var entity = new AccountingGenerationQueue
        {
            CreatedDate = _clock.UtcNow,
            RequestedBy = userId,
            GenerationMode = AccountingGenerationMode.MANUAL,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = QueueStatus.PENDING,
            Priority = dto.Priority,
            CashRegisterIdsJson = dto.CashRegisterIds is { Count: > 0 } ? JsonSerializer.Serialize(dto.CashRegisterIds) : null,
            RetryCount = 0
        };
        _db.AccountingGenerationQueues.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.CREATE, nameof(AccountingGenerationQueue), entity.Id,
            "Demande de génération manuelle mise en file d'attente", newValues: dto, ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task EnqueueAutomaticAsync(int cashRegisterId, DateTime start, DateTime end, int requestedBy, CancellationToken ct = default)
    {
        var settings = await _db.AccountingSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings is null || !settings.IsConfigured || settings.GenerationMode != AccountingGenerationMode.ON_CASH_CLOSING)
            return;

        var entity = new AccountingGenerationQueue
        {
            CreatedDate = _clock.UtcNow,
            RequestedBy = requestedBy,
            GenerationMode = AccountingGenerationMode.ON_CASH_CLOSING,
            StartDate = start,
            EndDate = end,
            Status = QueueStatus.PENDING,
            Priority = 0,
            CashRegisterIdsJson = JsonSerializer.Serialize(new[] { cashRegisterId }),
            RetryCount = 0
        };
        _db.AccountingGenerationQueues.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.CREATE, nameof(AccountingGenerationQueue), entity.Id,
            "Génération automatique mise en file d'attente (clôture de session)", ct: ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResponse<AccountingQueueItemDto>> ListAsync(AccountingQueueFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);

        var q = _db.AccountingGenerationQueues.AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.ResultGeneration)
            .AsQueryable();
        if (filter.Status.HasValue) q = q.Where(x => x.Status == filter.Status.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.Priority).ThenByDescending(x => x.CreatedDate)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);

        return new PagedResponse<AccountingQueueItemDto> { Items = items.Select(Map).ToList(), Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<AccountingQueueItemDto> GetAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.AccountingGenerationQueues.AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.ResultGeneration)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingGenerationQueue), id);
        return Map(item);
    }

    public async Task<AccountingQueueItemDto> CancelAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.AccountingGenerationQueues.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AccountingGenerationQueue), id);

        if (item.Status != QueueStatus.PENDING)
            throw new BusinessRuleException("QUEUE_NOT_CANCELLABLE", "Seule une demande en attente (PENDING) peut être annulée.");

        item.Status = QueueStatus.CANCELLED;
        item.CompletedDate = _clock.UtcNow;

        await _audit.LogAsync(AuditAction.CANCEL, nameof(AccountingGenerationQueue), item.Id, "Demande de génération annulée", ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private static AccountingQueueItemDto Map(AccountingGenerationQueue q) => new(
        q.Id, q.CreatedDate, q.RequestedBy, q.RequestedByUser.FullName,
        q.GenerationMode, q.StartDate, q.EndDate, q.Status, q.Priority,
        string.IsNullOrWhiteSpace(q.CashRegisterIdsJson) ? null : JsonSerializer.Deserialize<List<int>>(q.CashRegisterIdsJson),
        q.Remarks, q.RetryCount, q.StartedDate, q.CompletedDate,
        q.ResultGenerationId, q.ResultGeneration?.Reference);
}
