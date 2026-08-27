using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/accounting/dashboard")]
[Authorize(Roles = RoleCodes.Supervisor)]
public class AccountingDashboardController : ControllerBase
{
    private readonly IAccountingDashboardService _dashboard;
    private readonly IFeatureService _features;

    public AccountingDashboardController(IAccountingDashboardService dashboard, IFeatureService features)
    {
        _dashboard = dashboard;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<AccountingDashboardDto>> Get(CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _dashboard.GetAsync(ct));
    }
}
