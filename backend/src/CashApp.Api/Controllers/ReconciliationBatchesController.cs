using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Application.Reconciliation;
using CashApp.Application.Reconciliation.Dtos;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/reconciliation-batches")]
[Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
public class ReconciliationBatchesController : ControllerBase
{
    private readonly IReconciliationService _service;
    private readonly IFeatureService _features;

    public ReconciliationBatchesController(IReconciliationService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ReconciliationBatchListItemDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvReconciliation, ct);
        return Ok(await _service.ListAsync(page, pageSize, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReconciliationBatchDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvReconciliation, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<ReconciliationBatchDetailDto>> Create([FromBody] CreateReconciliationBatchDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvReconciliation, ct);
        var r = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPost("{id:int}/match")]
    public async Task<ActionResult<ReconciliationBatchDetailDto>> Match(int id, [FromBody] ReconcileItemsDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvReconciliation, ct);
        return Ok(await _service.MatchAsync(id, dto, ct));
    }
}
