using CashApp.Application.Dashboard.Dtos;

namespace CashApp.Application.Dashboard;

public interface IDashboardService
{
    Task<CashierDashboardDto> GetCashierDashboardAsync(int cashRegisterId, CancellationToken ct = default);
    Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(CancellationToken ct = default);
}
