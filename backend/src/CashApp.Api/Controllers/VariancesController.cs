using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Application.Variances;
using CashApp.Application.Variances.Dtos;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/variances")]
[Authorize]
public class VariancesController : ControllerBase
{
    private readonly IVarianceService _service;
    private readonly IFeatureService _features;

    public VariancesController(IVarianceService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<VarianceCaseListItemDto>>> List([FromQuery] VarianceFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvVarianceManagement, ct);
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VarianceCaseDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvVarianceManagement, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost("{id:int}/justify")]
    public async Task<ActionResult<VarianceCaseDetailDto>> Justify(int id, [FromBody] CreateVarianceJustificationDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvVarianceManagement, ct);
        return Ok(await _service.AddJustificationAsync(id, dto, ct));
    }
}
