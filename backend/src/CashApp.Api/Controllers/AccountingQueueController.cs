using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/accounting/queue")]
[Authorize(Roles = RoleCodes.Supervisor)]
public class AccountingQueueController : ControllerBase
{
    private readonly IAccountingQueueService _queue;
    private readonly IAccountingRetryService _retry;
    private readonly IFeatureService _features;

    public AccountingQueueController(IAccountingQueueService queue, IAccountingRetryService retry, IFeatureService features)
    {
        _queue = queue;
        _retry = retry;
        _features = features;
    }

    [HttpPost("manual")]
    public async Task<ActionResult<AccountingQueueItemDto>> GenerateManual([FromBody] EnqueueManualGenerationDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        var r = await _queue.EnqueueManualAsync(dto, ct);
        return CreatedAtAction(nameof(GetDetails), new { id = r.Id }, r);
    }

    [HttpPost("{id:int}/retry")]
    public async Task<ActionResult<AccountingQueueItemDto>> RetryQueue(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _retry.RetryAsync(id, ct));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<AccountingQueueItemDto>> CancelQueue(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _queue.CancelAsync(id, ct));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AccountingQueueItemDto>>> GetQueue([FromQuery] AccountingQueueFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _queue.ListAsync(filter, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountingQueueItemDto>> GetDetails(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _queue.GetAsync(id, ct));
    }
}
