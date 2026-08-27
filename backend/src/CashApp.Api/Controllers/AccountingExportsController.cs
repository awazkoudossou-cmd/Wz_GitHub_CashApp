using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/accounting/exports")]
[Authorize(Roles = RoleCodes.Supervisor)]
public class AccountingExportsController : ControllerBase
{
    private readonly IAccountingExportService _export;
    private readonly IFeatureService _features;

    public AccountingExportsController(IAccountingExportService export, IFeatureService features)
    {
        _export = export;
        _features = features;
    }

    [HttpPost("preview")]
    public async Task<ActionResult<AccountingExportPreviewDto>> Preview([FromBody] AccountingEntryFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _export.PreviewExportAsync(filter, ct));
    }

    [HttpPost("generations/{generationId:int}")]
    public async Task<IActionResult> ExportGeneration(int generationId, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        var result = await _export.ExportGenerationAsync(generationId, ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPost("entries")]
    public async Task<IActionResult> ExportEntries([FromBody] AccountingEntryFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        var result = await _export.ExportEntriesAsync(filter, ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPost("{logId:int}/reexport")]
    public async Task<IActionResult> Reexport(int logId, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        var result = await _export.ReexportAsync(logId, ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AccountingExportLogDto>>> ListLogs([FromQuery] AccountingExportLogFilterDto filter, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _export.ListLogsAsync(filter, ct));
    }

    [HttpGet("{logId:int}")]
    public async Task<ActionResult<AccountingExportDetailDto>> GetDetail(int logId, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        return Ok(await _export.GetLogDetailAsync(logId, ct));
    }

    [HttpGet("{logId:int}/download")]
    public async Task<IActionResult> Download(int logId, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        var result = await _export.DownloadLogAsync(logId, ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpDelete("{logId:int}")]
    public async Task<IActionResult> Delete(int logId, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAccounting, ct);
        await _export.DeleteExportAsync(logId, ct);
        return NoContent();
    }
}
