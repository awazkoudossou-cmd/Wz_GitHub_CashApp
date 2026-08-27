using CashApp.Application.Approvals;
using CashApp.Application.Approvals.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/approval-requests")]
[Authorize]
public class ApprovalRequestsController : ControllerBase
{
    private readonly IApprovalService _service;
    private readonly IFeatureService _features;

    public ApprovalRequestsController(IApprovalService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ApprovalRequestListItemDto>>> List([FromQuery] ApprovalRequestFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvValidation, ct);
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApprovalRequestDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvValidation, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<ActionResult<ApprovalRequestDetailDto>> Approve(int id, [FromBody] ApproveRequestDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvValidation, ct);
        return Ok(await _service.ApproveAsync(id, dto, ct));
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
    public async Task<ActionResult<ApprovalRequestDetailDto>> Reject(int id, [FromBody] RejectRequestDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvValidation, ct);
        return Ok(await _service.RejectAsync(id, dto, ct));
    }
}
