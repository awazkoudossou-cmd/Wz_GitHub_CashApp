using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/accounting/accounts")]
[Authorize(Roles = RoleCodes.Supervisor)]
public class AccountingAccountsController : ControllerBase
{
    private readonly IAccountingAccountService _service;
    private readonly IFeatureService _features;

    public AccountingAccountsController(IAccountingAccountService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AccountingAccountListItemDto>>> List([FromQuery] AccountingAccountFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountingAccountDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AccountingAccountDetailDto>> Create([FromBody] CreateAccountingAccountDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        var r = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AccountingAccountDetailDto>> Update(int id, [FromBody] UpdateAccountingAccountDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.UpdateAsync(id, dto, ct));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<AccountingAccountDetailDto>> UpdateStatus(int id, [FromBody] UpdateAccountingAccountStatusDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.UpdateStatusAsync(id, dto, ct));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
