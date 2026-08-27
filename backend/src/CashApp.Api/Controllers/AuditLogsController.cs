using CashApp.Application.AuditLogs;
using CashApp.Application.AuditLogs.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;
    private readonly IFeatureService _features;
    private readonly IExportService _export;

    public AuditLogsController(IAuditLogService service, IFeatureService features, IExportService export)
    {
        _service = service;
        _features = features;
        _export = export;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogListItemDto>>> List([FromQuery] AuditLogFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAuditLog, ct);
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuditLogDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAuditLog, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] AuditLogFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAuditLog, ct);
        var xlsx = await _export.ExportAuditLogsExcelAsync(
            filter.ActionType?.ToString(), filter.EntityType, filter.EntityId, filter.PerformedBy, filter.From, filter.To, ct);
        return File(xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"audit_log_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }
}
