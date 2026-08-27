using CashApp.Application.Attachments;
using CashApp.Application.Attachments.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/attachments")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _service;
    private readonly IFeatureService _features;

    public AttachmentsController(IAttachmentService service, IFeatureService features)
    {
        _service = service;
        _features = features;
    }

    [HttpGet("{entityType}/{entityId:int}")]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> List(string entityType, int entityId, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAttachments, ct);
        return Ok(await _service.ListForEntityAsync(entityType, entityId, ct));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<UploadAttachmentResponseDto>> Upload(
        [FromForm] string entityType,
        [FromForm] int entityId,
        [FromForm] IFormFile file,
        [FromForm] string? description,
        CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAttachments, ct);
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Fichier vide ou absent." });

        await using var stream = file.OpenReadStream();
        var input = new UploadFileInput(file.FileName, file.ContentType ?? "application/octet-stream", file.Length, stream);
        var result = await _service.UploadAsync(entityType, entityId, description, input, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAttachments, ct);
        var (stream, ct2, fileName) = await _service.DownloadAsync(id, ct);
        return File(stream, ct2, fileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _features.EnsureEnabledAsync(FeatureCodes.AdvAttachments, ct);
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
