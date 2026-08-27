using CashApp.Application.Accounting.Dtos;

namespace CashApp.Application.Accounting;

public interface IAccountingDashboardService
{
    Task<AccountingDashboardDto> GetAsync(CancellationToken ct = default);
}
