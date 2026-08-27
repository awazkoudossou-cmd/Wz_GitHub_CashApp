using CashApp.Application.Common.Models;
using CashApp.Application.Reconciliation.Dtos;

namespace CashApp.Application.Reconciliation;

public interface IReconciliationService
{
    Task<PagedResponse<ReconciliationBatchListItemDto>> ListAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<ReconciliationBatchDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<ReconciliationBatchDetailDto> CreateAsync(CreateReconciliationBatchDto dto, CancellationToken ct = default);
    Task<ReconciliationBatchDetailDto> MatchAsync(int id, ReconcileItemsDto dto, CancellationToken ct = default);
}
