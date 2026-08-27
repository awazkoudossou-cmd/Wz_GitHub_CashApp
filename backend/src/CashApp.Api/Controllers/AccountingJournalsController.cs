using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/accounting/journals")]
[Authorize(Roles = RoleCodes.Supervisor)]
public class AccountingJournalsController : ControllerBase
{
    private readonly IAccountingJournalService _service;
    private readonly IFeatureService _features;

    public AccountingJournalsController(IAccountingJournalService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountingJournalListItemDto>>> List(CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.ListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountingJournalDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AccountingJournalDetailDto>> Create([FromBody] CreateAccountingJournalDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        var r = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AccountingJournalDetailDto>> Update(int id, [FromBody] UpdateAccountingJournalDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.UpdateAsync(id, dto, ct));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<AccountingJournalDetailDto>> UpdateStatus(int id, [FromBody] UpdateAccountingJournalStatusDto dto, CancellationToken ct)
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
