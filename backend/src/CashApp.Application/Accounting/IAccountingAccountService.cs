using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Models;

namespace CashApp.Application.Accounting;

public interface IAccountingAccountService
{
    Task<PagedResponse<AccountingAccountListItemDto>> ListAsync(AccountingAccountFilterDto filter, CancellationToken ct = default);
    Task<AccountingAccountDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<AccountingAccountDetailDto> CreateAsync(CreateAccountingAccountDto dto, CancellationToken ct = default);
    Task<AccountingAccountDetailDto> UpdateAsync(int id, UpdateAccountingAccountDto dto, CancellationToken ct = default);
    Task<AccountingAccountDetailDto> UpdateStatusAsync(int id, UpdateAccountingAccountStatusDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
