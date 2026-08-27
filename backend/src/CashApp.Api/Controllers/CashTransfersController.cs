using CashApp.Application.CashTransfers;
using CashApp.Application.CashTransfers.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/cash-transfers")]
[Authorize]
public class CashTransfersController : ControllerBase
{
    private readonly ICashTransferService _service;
    private readonly IFeatureService _features;

    public CashTransfersController(ICashTransferService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CashTransferListItemDto>>> List([FromQuery] CashTransferFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvTransfers, ct);
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CashTransferDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvTransfers, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<CashTransferDetailDto>> Create([FromBody] CreateCashTransferDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvTransfers, ct);
        var result = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor},{RoleCodes.Cashier}")]
    public async Task<ActionResult<CashTransferDetailDto>> Complete(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvTransfers, ct);
        return Ok(await _service.CompleteAsync(id, ct));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<CashTransferDetailDto>> Cancel(int id, [FromBody] CancelCashTransferDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvTransfers, ct);
        return Ok(await _service.CancelAsync(id, dto, ct));
    }
}
