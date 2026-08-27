using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Models;

namespace CashApp.Application.Accounting;

public interface IAccountingQueueService
{
    Task<AccountingQueueItemDto> EnqueueManualAsync(EnqueueManualGenerationDto dto, CancellationToken ct = default);

    // Appelé par CashSessionService à la clôture. No-op silencieux si le mode ON_CASH_CLOSING
    // n'est pas configuré — la clôture de caisse ne doit jamais échouer à cause de la comptabilité.
    Task EnqueueAutomaticAsync(int cashRegisterId, DateTime start, DateTime end, int requestedBy, CancellationToken ct = default);

    Task<PagedResponse<AccountingQueueItemDto>> ListAsync(AccountingQueueFilterDto filter, CancellationToken ct = default);
    Task<AccountingQueueItemDto> GetAsync(int id, CancellationToken ct = default);
    Task<AccountingQueueItemDto> CancelAsync(int id, CancellationToken ct = default);
}
