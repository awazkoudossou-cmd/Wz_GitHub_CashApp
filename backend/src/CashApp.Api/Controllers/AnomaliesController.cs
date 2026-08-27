using CashApp.Application.Anomalies;
using CashApp.Application.Anomalies.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/anomalies")]
[Authorize]
public class AnomaliesController : ControllerBase
{
    private readonly IAnomalyService _service;
    private readonly IFeatureService _features;

    public AnomaliesController(IAnomalyService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AnomalyListItemDto>>> List([FromQuery] AnomalyFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAnomalies, ct);
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AnomalyDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAnomalies, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AnomalyDetailDto>> Create([FromBody] CreateAnomalyDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAnomalies, ct);
        var r = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<ActionResult<AnomalyDetailDto>> Assign(int id, [FromBody] AssignAnomalyDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAnomalies, ct);
        return Ok(await _service.AssignAsync(id, dto, ct));
    }

    [HttpPost("{id:int}/resolve")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<ActionResult<AnomalyDetailDto>> Resolve(int id, [FromBody] ResolveAnomalyDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAnomalies, ct);
        return Ok(await _service.ResolveAsync(id, dto, ct));
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<AnomalyDetailDto>> AddComment(int id, [FromBody] AddAnomalyCommentDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAnomalies, ct);
        return Ok(await _service.AddCommentAsync(id, dto, ct));
    }
}
