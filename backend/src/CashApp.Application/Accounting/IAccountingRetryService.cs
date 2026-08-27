using CashApp.Application.Accounting.Dtos;

namespace CashApp.Application.Accounting;

public interface IAccountingRetryService
{
    Task<AccountingQueueItemDto> RetryAsync(int queueId, CancellationToken ct = default);
}
