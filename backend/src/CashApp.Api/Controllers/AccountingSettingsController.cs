using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/accounting/settings")]
[Authorize(Roles = RoleCodes.Supervisor)]
public class AccountingSettingsController : ControllerBase
{
    private readonly IAccountingSettingsService _service;
    private readonly IFeatureService _features;

    public AccountingSettingsController(IAccountingSettingsService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<AccountingSettingsDto>> Get(CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.GetAsync(ct));
    }

    [HttpPut]
    public async Task<ActionResult<AccountingSettingsDto>> Update([FromBody] UpdateAccountingSettingsDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.UpdateAsync(dto, ct));
    }
}
