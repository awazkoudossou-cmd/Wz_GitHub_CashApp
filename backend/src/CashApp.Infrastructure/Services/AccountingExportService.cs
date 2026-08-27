using System.Diagnostics;
using System.Text.Json;
using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Common.Models;
using CashApp.Application.Settings;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Infrastructure.Services;

public class AccountingExportService : IAccountingExportService
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IAccountingEntryService _entries;
    private readonly ISettingsService _settings;
    private readonly IExcelExportService _excel;
    private readonly IAccountingDownloadService _download;

    public AccountingExportService(IAppDbContext db, IDateTimeProvider clock, ICurrentUserService currentUser,
        IAuditLogger audit, IAccountingEntryService entries, ISettingsService settings,
        IExcelExportService excel, IAccountingDownloadService download)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _entries = entries;
        _settings = settings;
        _excel = excel;
        _download = download;
    }

    public async Task<AccountingExportPreviewDto> PreviewExportAsync(AccountingEntryFilterDto filter, CancellationToken ct = default)
    {
        var entries = await _entries.ListAllAsync(filter, ct);

        var totalDebit = entries.Sum(e => e.Debit);
        var totalCredit = entries.Sum(e => e.Credit);

        return new AccountingExportPreviewDto(
            EntryCount: entries.Count,
            BatchCount: entries.Select(e => e.GenerationId).Distinct().Count(),
            AccountCount: entries.Select(e => e.AccountId).Distinct().Count(),
            JournalCount: entries.Select(e => e.JournalId).Distinct().Count(),
            PeriodStart: entries.Count == 0 ? null : entries.Min(e => e.OperationDate),
            PeriodEnd: entries.Count == 0 ? null : entries.Max(e => e.OperationDate),
            TotalDebit: totalDebit,
            TotalCredit: totalCredit,
            IsBalanced: totalDebit == totalCredit,
            EstimatedSizeBytes: 4096 + (long)entries.Count * 130);
    }

    public async Task<AccountingExportResult> ExportGenerationAsync(int generationId, CancellationToken ct = default)
    {
        var filter = BatchFilter(generationId);
        return await BuildAndPersistAsync(filter, AccountingExportType.Batch, generationId, markGenerationExported: true, ct);
    }

    public async Task<AccountingExportResult> ExportEntriesAsync(AccountingEntryFilterDto filter, CancellationToken ct = default)
    {
        return await BuildAndPersistAsync(filter, AccountingExportType.Ledger, filter.GenerationId, markGenerationExported: false, ct);
    }

    public async Task<AccountingExportResult> ReexportAsync(int logId, CancellationToken ct = default)
    {
        var original = await _db.AccountingExportLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == logId, ct)
            ?? throw new NotFoundException(nameof(AccountingExportLog), logId);
        if (original.Status == AccountingExportStatus.DELETED)
            throw new BusinessRuleException("EXPORT_DELETED", "Cet export a été supprimé et ne peut pas être réexporté.");

        var filter = string.IsNullOrWhiteSpace(original.FilterJson)
            ? BatchFilter(original.GenerationId ?? 0)
            : JsonSerializer.Deserialize<AccountingEntryFilterDto>(original.FilterJson) ?? BatchFilter(original.GenerationId ?? 0);

        var result = await BuildAndPersistAsync(filter, original.ExportType, original.GenerationId, markGenerationExported: false, ct, remarks: $"Réexport de {original.ExportNumber}");

        await _audit.LogAsync(AuditAction.CREATE, nameof(AccountingExportLog), original.Id,
            $"Réexport de {original.ExportNumber}", ct: ct);

        return result;
    }

    public async Task<PagedResponse<AccountingExportLogDto>> ListLogsAsync(AccountingExportLogFilterDto filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);

        var q = _db.AccountingExportLogs.AsNoTracking()
            .Include(l => l.ExportedByUser)
            .Include(l => l.Generation)
            .AsQueryable();

        if (filter.Status.HasValue) q = q.Where(l => l.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.ExportType)) q = q.Where(l => l.ExportType == filter.ExportType);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(l => l.ExportedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(l => new AccountingExportLogDto(
                l.Id, l.ExportNumber, l.ExportType, l.GenerationId, l.Generation != null ? l.Generation.Reference : null,
                l.GenerationType, l.GenerationMode,
                l.FileName, l.ExportedBy, l.ExportedByUser.FullName, l.ExportedAt, l.LineCount, l.Status))
            .ToListAsync(ct);

        return new PagedResponse<AccountingExportLogDto> { Items = items, Page = page, PageSize = size, TotalCount = total };
    }

    public async Task<AccountingExportDetailDto> GetLogDetailAsync(int logId, CancellationToken ct = default)
    {
        var log = await _db.AccountingExportLogs.AsNoTracking()
            .Include(l => l.ExportedByUser)
            .Include(l => l.Generation)
            .FirstOrDefaultAsync(l => l.Id == logId, ct)
            ?? throw new NotFoundException(nameof(AccountingExportLog), logId);

        string? filterDescription = null;
        if (!string.IsNullOrWhiteSpace(log.FilterJson))
        {
            var filter = JsonSerializer.Deserialize<AccountingEntryFilterDto>(log.FilterJson);
            if (filter is not null) filterDescription = await DescribeFilterAsync(filter, ct);
        }

        return new AccountingExportDetailDto(
            log.Id, log.ExportNumber, log.ExportType, log.GenerationId,
            log.Generation?.Reference, filterDescription,
            log.ExportedBy, log.ExportedByUser.FullName, log.ExportedAt, log.LineCount,
            log.ProcessingTimeMs, log.Status, log.DownloadedAt, log.Remarks);
    }

    public Task<AccountingExportResult> DownloadLogAsync(int logId, CancellationToken ct = default) => _download.DownloadAsync(logId, ct);

    public async Task DeleteExportAsync(int logId, CancellationToken ct = default)
    {
        var log = await _db.AccountingExportLogs.FirstOrDefaultAsync(l => l.Id == logId, ct)
            ?? throw new NotFoundException(nameof(AccountingExportLog), logId);
        if (log.Status == AccountingExportStatus.DELETED)
            throw new BusinessRuleException("EXPORT_ALREADY_DELETED", "Ce fichier d'export a déjà été supprimé.");

        if (File.Exists(log.FilePath))
        {
            try { File.Delete(log.FilePath); } catch { /* le statut est mis à jour même si la suppression physique échoue */ }
        }
        log.Status = AccountingExportStatus.DELETED;

        await _audit.LogAsync(AuditAction.DELETE, nameof(AccountingExportLog), log.Id,
            $"Suppression du fichier d'export {log.ExportNumber} (le batch et les écritures ne sont pas affectés)", ct: ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<AccountingExportResult> BuildAndPersistAsync(AccountingEntryFilterDto filter, string exportType,
        int? generationId, bool markGenerationExported, CancellationToken ct, string? remarks = null)
    {
        var sw = Stopwatch.StartNew();
        var entries = await _entries.ListAllAsync(filter, ct);
        if (entries.Count == 0)
            throw new BusinessRuleException("EXPORT_EMPTY", "Aucune écriture ne correspond aux critères sélectionnés.");

        var totalDebit = entries.Sum(e => e.Debit);
        var totalCredit = entries.Sum(e => e.Credit);
        if (totalDebit != totalCredit)
            throw new BusinessRuleException("EXPORT_UNBALANCED",
                $"Le total des débits ({totalDebit:N2}) ne correspond pas au total des crédits ({totalCredit:N2}). Export interdit tant que le brouillard n'est pas équilibré.");

        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        var userName = (await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct))?.FullName ?? string.Empty;
        var filterDescription = await DescribeFilterAsync(filter, ct);

        var model = new AccountingLedgerExportModel(
            entries.Select(e => new AccountingLedgerExportRow(
                e.EntryDate, e.OperationDate, e.JournalCode, e.Reference, e.PieceNumber,
                e.AccountNumber, e.AccountName, e.Description, e.Debit, e.Credit, e.UserName, e.BatchReference)).ToList(),
            _clock.UtcNow, userName, filterDescription);

        var content = _excel.BuildLedgerWorkbook(model);

        var now = _clock.UtcNow;
        AccountingGeneration? generation = null;
        string fileName;
        if (generationId.HasValue)
        {
            generation = await _db.AccountingGenerations.FirstOrDefaultAsync(g => g.Id == generationId.Value, ct)
                ?? throw new NotFoundException(nameof(AccountingGeneration), generationId.Value);
            fileName = $"ACCOUNTING_BATCH_{generation.Reference}.xlsx";
        }
        else
        {
            fileName = $"ACCOUNTING_{now:yyyyMMdd}_{now:HHmmss}.xlsx";
        }

        var root = await ResolveRootPathAsync(ct);
        Directory.CreateDirectory(root);
        var storedFileName = $"{now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(root, storedFileName);
        await File.WriteAllBytesAsync(filePath, content, ct);

        sw.Stop();

        if (generation is not null && markGenerationExported)
        {
            generation.Exported = true;
            generation.ExportedAt = now;
            if (generation.Status == AccountingGenerationStatus.GENERATED)
                generation.Status = AccountingGenerationStatus.EXPORTED;
        }

        var exportNumber = await NextExportNumberAsync(now, ct);
        var log = new AccountingExportLog
        {
            ExportNumber = exportNumber,
            ExportType = exportType,
            GenerationId = generationId,
            GenerationType = generation?.GenerationType,
            GenerationMode = generation?.GenerationMode,
            FileName = fileName,
            ContentType = XlsxContentType,
            FilePath = filePath,
            Status = AccountingExportStatus.GENERATED,
            FilterJson = JsonSerializer.Serialize(filter),
            ProcessingTimeMs = (int)sw.ElapsedMilliseconds,
            Remarks = remarks,
            ExportedBy = userId,
            ExportedAt = now,
            LineCount = entries.Count
        };
        _db.AccountingExportLogs.Add(log);

        await _audit.LogAsync(AuditAction.CREATE, nameof(AccountingExportLog), 0,
            $"Export {exportNumber} créé ({entries.Count} écriture(s), {(exportType == AccountingExportType.Batch ? "batch " + generation!.Reference : "brouillard filtré")})",
            ct: ct);
        await _db.SaveChangesAsync(ct);

        return new AccountingExportResult(content, XlsxContentType, fileName);
    }

    private async Task<string> ResolveRootPathAsync(CancellationToken ct)
    {
        var raw = await _settings.GetRawAsync(SettingKeys.AccountingExportsRootPath, ct);
        return Path.GetFullPath(string.IsNullOrWhiteSpace(raw) ? "./exports/accounting" : raw!);
    }

    private async Task<string> NextExportNumberAsync(DateTime now, CancellationToken ct)
    {
        var prefix = $"EXP-{now:yyyyMMdd}-";
        var existing = await _db.AccountingExportLogs.AsNoTracking()
            .Where(l => l.ExportNumber.StartsWith(prefix))
            .Select(l => l.ExportNumber)
            .ToListAsync(ct);
        var next = existing
            .Select(n => int.TryParse(n[prefix.Length..], out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
        return $"{prefix}{next:D5}";
    }

    private async Task<string> DescribeFilterAsync(AccountingEntryFilterDto filter, CancellationToken ct)
    {
        var parts = new List<string>();

        if (filter.GenerationId.HasValue)
        {
            var reference = await _db.AccountingGenerations.AsNoTracking()
                .Where(g => g.Id == filter.GenerationId.Value).Select(g => g.Reference).FirstOrDefaultAsync(ct);
            parts.Add($"Batch : {reference ?? $"#{filter.GenerationId}"}");
        }
        if (filter.From.HasValue) parts.Add($"Du {filter.From:dd/MM/yyyy}");
        if (filter.To.HasValue) parts.Add($"Au {filter.To:dd/MM/yyyy}");
        if (filter.JournalId.HasValue)
        {
            var code = await _db.AccountingJournals.AsNoTracking()
                .Where(j => j.Id == filter.JournalId.Value).Select(j => j.Code).FirstOrDefaultAsync(ct);
            parts.Add($"Journal : {code ?? $"#{filter.JournalId}"}");
        }
        if (filter.AccountId.HasValue)
        {
            var number = await _db.AccountingAccounts.AsNoTracking()
                .Where(a => a.Id == filter.AccountId.Value).Select(a => a.AccountNumber).FirstOrDefaultAsync(ct);
            parts.Add($"Compte : {number ?? $"#{filter.AccountId}"}");
        }
        if (filter.CashRegisterId.HasValue)
        {
            var code = await _db.CashRegisters.AsNoTracking()
                .Where(c => c.Id == filter.CashRegisterId.Value).Select(c => c.Code).FirstOrDefaultAsync(ct);
            parts.Add($"Caisse : {code ?? $"#{filter.CashRegisterId}"}");
        }
        if (filter.CategoryId.HasValue)
        {
            var code = await _db.Categories.AsNoTracking()
                .Where(c => c.Id == filter.CategoryId.Value).Select(c => c.Code).FirstOrDefaultAsync(ct);
            parts.Add($"Catégorie : {code ?? $"#{filter.CategoryId}"}");
        }
        if (filter.UserId.HasValue)
        {
            var name = await _db.Users.AsNoTracking()
                .Where(u => u.Id == filter.UserId.Value).Select(u => u.FullName).FirstOrDefaultAsync(ct);
            parts.Add($"Utilisateur : {name ?? $"#{filter.UserId}"}");
        }
        if (filter.Locked.HasValue) parts.Add(filter.Locked.Value ? "État : verrouillé" : "État : modifiable");
        if (filter.GenerationType.HasValue) parts.Add($"Type génération : {filter.GenerationType}");
        if (filter.GenerationMode.HasValue) parts.Add($"Mode génération : {filter.GenerationMode}");
        if (!string.IsNullOrWhiteSpace(filter.Search)) parts.Add($"Recherche : {filter.Search}");

        return parts.Count == 0 ? "Aucun filtre (toutes les écritures)" : string.Join(" | ", parts);
    }

    private static AccountingEntryFilterDto BatchFilter(int generationId) => new(
        From: null, To: null, JournalId: null, AccountId: null, CashRegisterId: null, CategoryId: null,
        UserId: null, GenerationId: generationId, Locked: null, Reference: null, PieceNumber: null,
        Search: null, SortBy: null);
}
