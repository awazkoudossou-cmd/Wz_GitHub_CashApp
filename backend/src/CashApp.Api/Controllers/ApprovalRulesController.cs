using CashApp.Application.Approvals;
using CashApp.Application.Approvals.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/approval-rules")]
[Authorize(Roles = RoleCodes.Admin)]
public class ApprovalRulesController : ControllerBase
{
    private readonly IApprovalRuleService _service;
    private readonly IFeatureService _features;

    public ApprovalRulesController(IApprovalRuleService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApprovalRuleDto>>> List(CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvValidation, ct);
        return Ok(await _service.ListAsync(ct));
    }

    [HttpPost]
    public async Task<ActionResult<ApprovalRuleDto>> Create([FromBody] CreateApprovalRuleDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvValidation, ct);
        return Ok(await _service.CreateAsync(dto, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApprovalRuleDto>> Update(int id, [FromBody] UpdateApprovalRuleDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvValidation, ct);
        return Ok(await _service.UpdateAsync(id, dto, ct));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateApprovalRuleStatusDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvValidation, ct);
        await _service.UpdateStatusAsync(id, dto, ct);
        return NoContent();
    }
}
