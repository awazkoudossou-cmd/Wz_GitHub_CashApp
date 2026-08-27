using CashApp.Application.BankDeposits;
using CashApp.Application.BankDeposits.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/bank-deposits")]
[Authorize]
public class BankDepositsController : ControllerBase
{
    private readonly IBankDepositService _service;
    private readonly IFeatureService _features;

    public BankDepositsController(IBankDepositService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<BankDepositListItemDto>>> List([FromQuery] BankDepositFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvBankDeposits, ct);
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BankDepositDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvBankDeposits, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<BankDepositDetailDto>> Create([FromBody] CreateBankDepositDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvBankDeposits, ct);
        var r = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<BankDepositDetailDto>> Complete(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvBankDeposits, ct);
        return Ok(await _service.CompleteAsync(id, ct));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<BankDepositDetailDto>> Cancel(int id, [FromBody] CancelBankDepositDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvBankDeposits, ct);
        return Ok(await _service.CancelAsync(id, dto, ct));
    }
}
