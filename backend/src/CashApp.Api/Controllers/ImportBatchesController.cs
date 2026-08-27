using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Application.Imports;
using CashApp.Application.Imports.Dtos;
using CashApp.Domain.Constants;
using CashApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/import-batches")]
[Authorize(Roles = $"{RoleCodes.Admin},{RoleCodes.Supervisor}")]
public class ImportBatchesController : ControllerBase
{
    private readonly IImportService _service;
    private readonly IFeatureService _features;

    public ImportBatchesController(IImportService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ImportBatchListItemDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvImports, ct);
        return Ok(await _service.ListAsync(page, pageSize, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ImportBatchDetailDto>> Get(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvImports, ct);
        return Ok(await _service.GetAsync(id, ct));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<ImportBatchDetailDto>> Upload(
        [FromForm] ImportBatchType batchType,
        [FromForm] IFormFile file,
        [FromForm] int? cashRegisterId,
        CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvImports, ct);
        if (file is null || file.Length == 0) return BadRequest(new { message = "Fichier vide ou absent." });
        await using var stream = file.OpenReadStream();
        var batch = await _service.UploadAsync(batchType, cashRegisterId, file.FileName, stream, ct);
        return Ok(batch);
    }

    [HttpPost("{id:int}/preview")]
    public async Task<ActionResult<ImportPreviewDto>> Preview(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvImports, ct);
        return Ok(await _service.PreviewAsync(id, ct));
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<ImportBatchDetailDto>> Confirm(int id, [FromBody] ConfirmImportDto dto, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvImports, ct);
        return Ok(await _service.ConfirmAsync(id, dto, ct));
    }
}
