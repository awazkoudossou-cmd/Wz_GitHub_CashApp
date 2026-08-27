using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/accounting/entries")]
[Authorize(Roles = RoleCodes.Supervisor)]
public class AccountingEntriesController : ControllerBase
{
    private readonly IAccountingEntryService _service;
    private readonly IFeatureService _features;

    public AccountingEntriesController(IAccountingEntryService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AccountingEntryListItemDto>>> List([FromQuery] AccountingEntryFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AccountingLedgerStatsDto>> Stats(CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.GetStatsAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountingEntryDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<AccountingEntryDetailDto>> Update(int id, [FromBody] UpdateAccountingEntryDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.UpdateAsync(id, dto, ct));
    }
}
