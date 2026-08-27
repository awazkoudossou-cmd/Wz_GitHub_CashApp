using CashApp.Application.Accounting.Dtos;

namespace CashApp.Application.Accounting;

public interface IAccountingJournalService
{
    Task<IReadOnlyList<AccountingJournalListItemDto>> ListAsync(CancellationToken ct = default);
    Task<AccountingJournalDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<AccountingJournalDetailDto> CreateAsync(CreateAccountingJournalDto dto, CancellationToken ct = default);
    Task<AccountingJournalDetailDto> UpdateAsync(int id, UpdateAccountingJournalDto dto, CancellationToken ct = default);
    Task<AccountingJournalDetailDto> UpdateStatusAsync(int id, UpdateAccountingJournalStatusDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
