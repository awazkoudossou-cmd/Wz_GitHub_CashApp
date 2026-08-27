using CashApp.Application.Common.Interfaces;
using CashApp.Application.Reports;
using CashApp.Application.Reports.Dtos;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
public class ReportsController : ControllerBase
{
    private readonly IAdvancedReportService _service;
    private readonly IFeatureService _features;

    public ReportsController(IAdvancedReportService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet("cash")]
    public async Task<ActionResult<CashReportResultDto>> Cash([FromQuery] CashReportFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAdvancedReports, ct);
        return Ok(await _service.CashAsync(filter, ct));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<CategoryReportResultDto>> Categories([FromQuery] CategoryReportFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAdvancedReports, ct);
        return Ok(await _service.CategoriesAsync(filter, ct));
    }

    [HttpGet("variances")]
    public async Task<ActionResult<VarianceReportResultDto>> Variances([FromQuery] VarianceReportFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAdvancedReports, ct);
        return Ok(await _service.VariancesAsync(filter, ct));
    }

    [HttpGet("transfers")]
    public async Task<ActionResult<TransferReportResultDto>> Transfers([FromQuery] TransferReportFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAdvancedReports, ct);
        return Ok(await _service.TransfersAsync(filter, ct));
    }

    [HttpGet("deposits")]
    public async Task<ActionResult<DepositReportResultDto>> Deposits([FromQuery] DepositReportFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAdvancedReports, ct);
        return Ok(await _service.DepositsAsync(filter, ct));
    }

    [HttpGet("anomalies")]
    public async Task<ActionResult<AnomalyReportResultDto>> Anomalies([FromQuery] AnomalyReportFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAdvancedReports, ct);
        return Ok(await _service.AnomaliesAsync(filter, ct));
    }

    [HttpGet("approvals")]
    public async Task<ActionResult<ApprovalReportResultDto>> Approvals([FromQuery] ApprovalReportFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAdvancedReports, ct);
        return Ok(await _service.ApprovalsAsync(filter, ct));
    }
}
