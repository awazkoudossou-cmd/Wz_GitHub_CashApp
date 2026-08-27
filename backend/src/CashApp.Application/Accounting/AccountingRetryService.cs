using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Accounting;

public class AccountingRetryService : IAccountingRetryService
{
    public const int MaxRetries = 3;

    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;
    private readonly IAccountingQueueService _queue;

    public AccountingRetryService(IAppDbContext db, IDateTimeProvider clock, IAuditLogger audit, IAccountingQueueService queue)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
        _queue = queue;
    }

    public async Task<AccountingQueueItemDto> RetryAsync(int queueId, CancellationToken ct = default)
    {
        var item = await _db.AccountingGenerationQueues.FirstOrDefaultAsync(x => x.Id == queueId, ct)
            ?? throw new NotFoundException(nameof(AccountingGenerationQueue), queueId);

        if (item.Status != QueueStatus.FAILED)
            throw new BusinessRuleException("QUEUE_NOT_FAILED", "Seule une demande en échec (FAILED) peut être relancée.");
        if (item.RetryCount >= MaxRetries)
            throw new BusinessRuleException("QUEUE_MAX_RETRIES", $"Nombre maximal de tentatives atteint ({MaxRetries}) — échec définitif.");

        item.RetryCount += 1;
        item.Status = QueueStatus.PENDING;
        item.StartedDate = null;
        item.CompletedDate = null;

        await _audit.LogAsync(AuditAction.UPDATE, nameof(AccountingGenerationQueue), item.Id,
            $"Relance de la génération en file (tentative {item.RetryCount}/{MaxRetries})",
            metadata: new { item.RetryCount }, ct: ct);
        await _db.SaveChangesAsync(ct);

        return await _queue.GetAsync(queueId, ct);
    }
}
