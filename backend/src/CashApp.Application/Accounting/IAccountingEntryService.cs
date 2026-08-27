using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Models;

namespace CashApp.Application.Accounting;

public interface IAccountingEntryService
{
    Task<PagedResponse<AccountingEntryListItemDto>> ListAsync(AccountingEntryFilterDto filter, CancellationToken ct = default);
    Task<IReadOnlyList<AccountingEntryListItemDto>> ListAllAsync(AccountingEntryFilterDto filter, CancellationToken ct = default);
    Task<AccountingEntryDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<AccountingEntryDetailDto> UpdateAsync(int id, UpdateAccountingEntryDto dto, CancellationToken ct = default);
    Task<AccountingLedgerStatsDto> GetStatsAsync(CancellationToken ct = default);
}
