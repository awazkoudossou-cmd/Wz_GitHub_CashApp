using CashApp.Application.Backups.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using CashApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/backups")]
[Authorize(Roles = RoleCodes.Admin)]
public class BackupsController : ControllerBase
{
    private readonly IBackupService _backup;
    private readonly AppDbContext _db;

    public BackupsController(IBackupService backup, AppDbContext db)
    {
        _backup = backup;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BackupListItemDto>>> List(CancellationToken ct)
    {
        var logs = await _db.BackupLogs.AsNoTracking()
            .Include(b => b.CreatedByUser)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        var items = logs.Select(b => new BackupListItemDto(
            b.Id, b.FileName, b.FilePath, b.CreatedBy, b.CreatedByUser?.FullName,
            b.CreatedAt, GetSize(b.FilePath))).ToList();
        return Ok(items);
    }

    [HttpPost("create")]
    public async Task<ActionResult<CreateBackupResponseDto>> Create(CancellationToken ct)
    {
        var path = await _backup.CreateBackupAsync(ct);
        var log = await _db.BackupLogs.OrderByDescending(b => b.Id).FirstAsync(ct);
        return Ok(new CreateBackupResponseDto(log.Id, log.FileName, log.FilePath, log.CreatedAt));
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore([FromBody] RestoreBackupDto dto, CancellationToken ct)
    {
        var log = await _db.BackupLogs.AsNoTracking()
            .FirstOrDefaultAsync(b => b.FileName == dto.FileName, ct);
        var path = log?.FilePath ?? dto.FileName;
        await _backup.RestoreBackupAsync(path, ct);
        return Ok(new { restored = true, path });
    }

    private static long? GetSize(string path)
    {
        try { return System.IO.File.Exists(path) ? new FileInfo(path).Length : (long?)null; }
        catch { return null; }
    }
}
