using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Models;

namespace CashApp.Application.Accounting;

// Fait office de "AccountingBatchService" : AccountingGeneration porte le rôle de Batch
// (BatchNumber = Reference, GenerationDate = GeneratedAt — voir V3_3).
public interface IAccountingGenerationService
{
    Task<PagedResponse<AccountingGenerationListItemDto>> ListAsync(AccountingGenerationFilterDto filter, CancellationToken ct = default);
    Task<AccountingGenerationDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<AccountingGenerationDetailDto> CancelAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<PagedResponse<AccountingPendingDto>> ListPendingAsync(AccountingPendingFilterDto filter, CancellationToken ct = default);
}
