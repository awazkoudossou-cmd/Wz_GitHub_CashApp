using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/accounting")]
[Authorize(Roles = RoleCodes.Supervisor)]
public class AccountingGenerationsController : ControllerBase
{
    private readonly IAccountingGenerationService _service;
    private readonly IAccountingGenerationEngineService _engine;
    private readonly IFeatureService _features;

    public AccountingGenerationsController(IAccountingGenerationService service, IAccountingGenerationEngineService engine, IFeatureService features)
    {
        _service = service;
        _engine = engine;
        _features = features;
    }

    [HttpPost("generations/generate")]
    public async Task<ActionResult<AccountingGenerationDetailDto>> Generate([FromBody] GenerateAccountingEntriesDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        var r = await _engine.GenerateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPost("generations/generate-pending")]
    public async Task<ActionResult<AccountingGenerationDetailDto>> GeneratePending(CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _engine.GeneratePendingAsync(ct));
    }

    [HttpGet("generations/preview")]
    public async Task<ActionResult<AccountingGenerationPreviewDto>> Preview(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int[]? cashRegisterIds, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        var dto = new GenerateAccountingEntriesDto(startDate, endDate, cashRegisterIds is { Length: > 0 } ? cashRegisterIds : null);
        return Ok(await _engine.PreviewAsync(dto, ct));
    }

    [HttpGet("generations")]
    public async Task<ActionResult<PagedResponse<AccountingGenerationListItemDto>>> List([FromQuery] AccountingGenerationFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("generations/{id:int}")]
    public async Task<ActionResult<AccountingGenerationDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost("generations/{id:int}/cancel")]
    public async Task<ActionResult<AccountingGenerationDetailDto>> Cancel(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.CancelAsync(id, ct));
    }

    [HttpDelete("generations/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("generations/{id:int}/regenerate")]
    public async Task<ActionResult<AccountingGenerationDetailDto>> Regenerate(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _engine.RegenerateAsync(id, ct));
    }

    [HttpGet("pending")]
    public async Task<ActionResult<PagedResponse<AccountingPendingDto>>> ListPending([FromQuery] AccountingPendingFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _service.ListPendingAsync(filter, ct));
    }
}
