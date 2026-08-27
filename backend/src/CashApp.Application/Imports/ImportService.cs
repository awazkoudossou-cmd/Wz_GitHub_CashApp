using System.Text.Json;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Application.Imports.Dtos;
using CashApp.Application.Settings;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Imports;

public class ImportService : IImportService
{
    private readonly IAppDbContext _db;
    private readonly ISettingsService _settings;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;
    private readonly IEnumerable<IImportParser> _parsers;

    public ImportService(IAppDbContext db, ISettingsService settings, ICurrentUserService currentUser,
        IDateTimeProvider clock, IAuditLogger audit, IEnumerable<IImportParser> parsers)
    {
        _db = db; _settings = settings; _currentUser = currentUser; _clock = clock; _audit = audit; _parsers = parsers;
    }

    public async Task<PagedResponse<ImportBatchListItemDto>> ListAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.ImportBatches.AsNoTracking().Include(b => b.UploadedByUser);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(b => b.UploadedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new ImportBatchListItemDto(
                b.Id, b.BatchRef, b.BatchType, b.OriginalFileName,
                b.UploadedBy, b.UploadedByUser.FullName, b.UploadedAt,
                b.Status, b.TotalLines, b.ValidLines, b.InvalidLines, b.ImportedLines,
                b.CashRegisterId))
            .ToListAsync(ct);
        return new PagedResponse<ImportBatchListItemDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<ImportBatchDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var b = await _db.ImportBatches.AsNoTracking()
            .Include(x => x.UploadedByUser)
            .Include(x => x.CashRegister)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(ImportBatch), id);
        return MapDetail(b);
    }

    public async Task<ImportBatchDetailDto> UploadAsync(ImportBatchType batchType, int? cashRegisterId,
        string originalFileName, Stream content, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        var rootSetting = await _settings.GetRawAsync(SettingKeys.ImportsRootPath, ct);
        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(rootSetting) ? "./imports" : rootSetting!);
        Directory.CreateDirectory(root);

        var safeName = Path.GetFileName(originalFileName);
        var stored = Path.Combine(root, $"{_clock.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{safeName}");
        await using (var fs = File.Create(stored))
            await content.CopyToAsync(fs, ct);

        var entity = new ImportBatch
        {
            BatchRef = $"IB-{_clock.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            BatchType = batchType,
            OriginalFileName = safeName,
            StoredPath = stored,
            UploadedBy = userId,
            UploadedAt = _clock.UtcNow,
            Status = ImportBatchStatus.UPLOADED,
            CashRegisterId = cashRegisterId
        };
        _db.ImportBatches.Add(entity);
        await _audit.LogAsync(AuditAction.CREATE, nameof(ImportBatch), 0, $"Upload {safeName} ({batchType})", ct: ct);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<ImportPreviewDto> PreviewAsync(int id, CancellationToken ct = default)
    {
        var batch = await _db.ImportBatches.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(ImportBatch), id);

        var parser = _parsers.FirstOrDefault(p => p.TargetType == batch.BatchType)
            ?? throw new BusinessRuleException("IMPORT_PARSER_MISSING", $"Aucun parser pour {batch.BatchType}.");

        if (!File.Exists(batch.StoredPath))
            throw new BusinessRuleException("IMPORT_FILE_MISSING", "Fichier source introuvable.");

        // Purge des lignes preview précédentes.
        var oldLines = _db.ImportBatchLines.Where(l => l.ImportBatchId == batch.Id);
        _db.ImportBatchLines.RemoveRange(oldLines);
        await _db.SaveChangesAsync(ct);

        var ext = Path.GetExtension(batch.StoredPath).TrimStart('.').ToLowerInvariant();
        var errors = new List<string>();
        int valid = 0, invalid = 0, total = 0;

        await using (var stream = File.OpenRead(batch.StoredPath))
        {
            await foreach (var row in parser.ReadAsync(stream, ext, ct))
            {
                total++;
                var validation = await parser.ValidateAsync(row, batch.CashRegisterId, ct);
                var line = new ImportBatchLine
                {
                    ImportBatchId = batch.Id,
                    LineNumber = row.LineNumber,
                    RawDataJson = JsonSerializer.Serialize(row.Fields),
                    ParsedDataJson = validation.ParsedData is null ? null : JsonSerializer.Serialize(validation.ParsedData),
                    Status = validation.IsValid ? ImportLineStatus.VALID : ImportLineStatus.INVALID,
                    ErrorMessage = validation.ErrorMessage
                };
                _db.ImportBatchLines.Add(line);
                if (validation.IsValid) valid++; else { invalid++; errors.Add($"L{row.LineNumber}: {validation.ErrorMessage}"); }
            }
        }

        batch.TotalLines = total;
        batch.ValidLines = valid;
        batch.InvalidLines = invalid;
        batch.Status = ImportBatchStatus.PREVIEWED;
        batch.ErrorSummaryJson = errors.Count > 0 ? JsonSerializer.Serialize(errors) : null;
        await _audit.LogAsync(AuditAction.UPDATE, nameof(ImportBatch), batch.Id,
            $"Preview {batch.BatchRef}: {valid} OK / {invalid} KO sur {total}", ct: ct);
        await _db.SaveChangesAsync(ct);

        var detail = await GetAsync(batch.Id, ct);
        return new ImportPreviewDto(batch.Id, total, valid, invalid, detail.Lines);
    }

    public async Task<ImportBatchDetailDto> ConfirmAsync(int id, ConfirmImportDto dto, CancellationToken ct = default)
    {
        var batch = await _db.ImportBatches.Include(b => b.Lines).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(ImportBatch), id);

        if (batch.Status is not (ImportBatchStatus.PREVIEWED or ImportBatchStatus.PARTIAL))
            throw new BusinessRuleException("IMPORT_INVALID_STATUS", $"Batch en statut '{batch.Status}'.");

        var parser = _parsers.FirstOrDefault(p => p.TargetType == batch.BatchType)
            ?? throw new BusinessRuleException("IMPORT_PARSER_MISSING", $"Aucun parser pour {batch.BatchType}.");

        var allowPartial = dto.AllowPartialSuccess
            || (bool.TryParse(await _settings.GetRawAsync(SettingKeys.ImportAllowPartialSuccess, ct), out var b) && b);

        if (batch.InvalidLines > 0 && !allowPartial)
            throw new BusinessRuleException("IMPORT_HAS_ERRORS", $"{batch.InvalidLines} lignes invalides. Activer AllowPartialSuccess pour importer les valides uniquement.");

        int imported = 0, failed = 0;
        var lines = batch.Lines.Where(l => l.Status == ImportLineStatus.VALID && l.ParsedDataJson != null).OrderBy(l => l.LineNumber).ToList();

        foreach (var line in lines)
        {
            try
            {
                // Désérialisation typée selon le batchType.
                object parsed = batch.BatchType switch
                {
                    ImportBatchType.CASH_OPERATIONS => JsonSerializer.Deserialize<Parsers.CashOperationParsed>(line.ParsedDataJson!)!,
                    ImportBatchType.CATEGORIES => JsonSerializer.Deserialize<Parsers.CategoryParsed>(line.ParsedDataJson!)!,
                    ImportBatchType.ACCOUNTING_ACCOUNTS => JsonSerializer.Deserialize<Parsers.AccountingAccountParsed>(line.ParsedDataJson!)!,
                    _ => throw new InvalidOperationException($"Type {batch.BatchType} non implémenté.")
                };
                var (entityType, entityId) = await parser.ImportAsync(parsed, ct);
                line.Status = ImportLineStatus.IMPORTED;
                line.CreatedEntityType = entityType;
                line.CreatedEntityId = entityId;
                imported++;
            }
            catch (Exception ex)
            {
                line.Status = ImportLineStatus.INVALID;
                line.ErrorMessage = $"Import failed: {ex.Message}";
                failed++;
            }
        }

        batch.ImportedLines = imported;
        batch.Status = failed > 0 ? ImportBatchStatus.PARTIAL : ImportBatchStatus.CONFIRMED;
        await _audit.LogAsync(AuditAction.COMPLETE, nameof(ImportBatch), batch.Id,
            $"Confirm {batch.BatchRef}: {imported} importées, {failed} échouées",
            metadata: new { batch.BatchType, imported, failed }, ct: ct);
        await _db.SaveChangesAsync(ct);

        return await GetAsync(batch.Id, ct);
    }

    private static ImportBatchDetailDto MapDetail(ImportBatch b) =>
        new(b.Id, b.BatchRef, b.BatchType, b.OriginalFileName,
            b.UploadedBy, b.UploadedByUser.FullName, b.UploadedAt,
            b.Status, b.TotalLines, b.ValidLines, b.InvalidLines, b.ImportedLines,
            b.CashRegisterId, b.CashRegister?.Code, b.ErrorSummaryJson,
            b.Lines.OrderBy(l => l.LineNumber)
                .Select(l => new ImportPreviewLineDto(l.LineNumber, l.Status, l.ErrorMessage, l.RawDataJson, l.ParsedDataJson, l.CreatedEntityId))
                .ToList());
}
