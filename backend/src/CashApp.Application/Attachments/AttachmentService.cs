using CashApp.Application.Attachments.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Attachments;

public class AttachmentService : IAttachmentService
{
    private readonly IAppDbContext _db;
    private readonly ISettingsService _settings;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;

    public AttachmentService(IAppDbContext db, ISettingsService settings, ICurrentUserService currentUser,
        IDateTimeProvider clock, IAuditLogger audit)
    {
        _db = db; _settings = settings; _currentUser = currentUser; _clock = clock; _audit = audit;
    }

    public async Task<IReadOnlyList<AttachmentDto>> ListForEntityAsync(string entityType, int entityId, CancellationToken ct = default)
    {
        return await _db.Attachments.AsNoTracking()
            .Include(a => a.UploadedByUser)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => Map(a))
            .ToListAsync(ct);
    }

    public async Task<AttachmentDto> GetAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.Attachments.AsNoTracking()
            .Include(x => x.UploadedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(Attachment), id);
        return Map(a);
    }

    public async Task<UploadAttachmentResponseDto> UploadAsync(string entityType, int entityId, string? description, UploadFileInput file, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");

        var ext = Path.GetExtension(file.OriginalFileName).TrimStart('.').ToLowerInvariant();
        var allowed = (await _settings.GetRawAsync(SettingKeys.AllowedAttachmentExtensions, ct) ?? "pdf,jpg,png")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();
        if (!allowed.Contains(ext))
            throw new BusinessRuleException("ATTACHMENT_EXTENSION_FORBIDDEN", $"Extension '.{ext}' non autorisée.");

        var maxMb = int.TryParse(await _settings.GetRawAsync(SettingKeys.MaxAttachmentSizeMb, ct), out var m) ? m : 10;
        if (file.Length > (long)maxMb * 1024 * 1024)
            throw new BusinessRuleException("ATTACHMENT_TOO_LARGE", $"Fichier > {maxMb} Mo.");

        var rootSetting = await _settings.GetRawAsync(SettingKeys.AttachmentsRootPath, ct);
        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(rootSetting) ? "./attachments" : rootSetting!);
        var subDir = Path.Combine(root, entityType, entityId.ToString());
        Directory.CreateDirectory(subDir);

        var safeOriginal = SafeFileName(file.OriginalFileName);
        var storedName = $"{_clock.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}_{safeOriginal}";
        var storedPath = Path.Combine(subDir, storedName);

        await using (var fs = File.Create(storedPath))
        {
            await file.Content.CopyToAsync(fs, ct);
        }

        var entity = new Attachment
        {
            FileName = storedName,
            OriginalFileName = safeOriginal,
            ContentType = file.ContentType,
            FileSize = file.Length,
            StoredPath = storedPath,
            EntityType = entityType,
            EntityId = entityId,
            UploadedBy = userId,
            UploadedAt = _clock.UtcNow,
            Description = description?.Trim()
        };
        _db.Attachments.Add(entity);
        await _audit.LogAsync(AuditAction.CREATE, nameof(Attachment), 0,
            $"Upload '{safeOriginal}' on {entityType}#{entityId}",
            metadata: new { entityType, entityId, size = file.Length, ext }, ct: ct);
        await _db.SaveChangesAsync(ct);

        var loaded = await _db.Attachments.AsNoTracking()
            .Include(x => x.UploadedByUser)
            .FirstAsync(x => x.Id == entity.Id, ct);
        return new UploadAttachmentResponseDto(Map(loaded));
    }

    public async Task<(Stream Content, string ContentType, string OriginalFileName)> DownloadAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.Attachments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(Attachment), id);
        if (!File.Exists(a.StoredPath))
            throw new NotFoundException(nameof(Attachment), id);
        return (File.OpenRead(a.StoredPath), a.ContentType, a.OriginalFileName);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.Attachments.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(Attachment), id);
        a.IsDeleted = true; // soft delete (le fichier reste sur disque pour audit/possibilité de restauration manuelle)
        await _audit.LogAsync(AuditAction.DELETE, nameof(Attachment), a.Id, $"Soft delete {a.OriginalFileName}", ct: ct);
        await _db.SaveChangesAsync(ct);
    }

    private static AttachmentDto Map(Attachment a) =>
        new(a.Id, a.FileName, a.OriginalFileName, a.ContentType, a.FileSize, a.EntityType, a.EntityId,
            a.UploadedBy, a.UploadedByUser.FullName, a.UploadedAt, a.Description);

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return clean.Length > 200 ? clean[..200] : clean;
    }
}
