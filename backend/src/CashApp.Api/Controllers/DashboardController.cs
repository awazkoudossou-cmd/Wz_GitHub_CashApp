using CashApp.Application.Dashboard;
using CashApp.Application.Dashboard.Dtos;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;
    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("cashier")]
    public async Task<ActionResult<CashierDashboardDto>> Cashier([FromQuery] int cashRegisterId, CancellationToken ct)
        => Ok(await _service.GetCashierDashboardAsync(cashRegisterId, ct));

    [HttpGet("supervisor")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<ActionResult<SupervisorDashboardDto>> Supervisor(CancellationToken ct)
        => Ok(await _service.GetSupervisorDashboardAsync(ct));
}
