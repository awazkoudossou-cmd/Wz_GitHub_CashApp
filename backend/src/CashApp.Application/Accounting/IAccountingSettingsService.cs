using CashApp.Application.Accounting.Dtos;

namespace CashApp.Application.Accounting;

public interface IAccountingSettingsService
{
    Task<AccountingSettingsDto> GetAsync(CancellationToken ct = default);
    Task<AccountingSettingsDto> UpdateAsync(UpdateAccountingSettingsDto dto, CancellationToken ct = default);
}
