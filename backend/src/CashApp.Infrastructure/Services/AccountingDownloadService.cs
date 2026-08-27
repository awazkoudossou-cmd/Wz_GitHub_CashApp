using CashApp.Application.Accounting;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Infrastructure.Services;

public interface IAccountingDownloadService
{
    Task<AccountingExportResult> DownloadAsync(int logId, CancellationToken ct = default);
}

// Lit le fichier Excel depuis le disque et fait transitionner le statut GENERATED -> DOWNLOADED
// (au premier téléchargement uniquement) tout en traçant l'action dans l'audit.
public class AccountingDownloadService : IAccountingDownloadService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;

    public AccountingDownloadService(IAppDbContext db, IDateTimeProvider clock, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
    }

    public async Task<AccountingExportResult> DownloadAsync(int logId, CancellationToken ct = default)
    {
        var log = await _db.AccountingExportLogs.FirstOrDefaultAsync(l => l.Id == logId, ct)
            ?? throw new NotFoundException(nameof(AccountingExportLog), logId);

        if (log.Status == AccountingExportStatus.DELETED)
            throw new BusinessRuleException("EXPORT_DELETED", "Ce fichier d'export a été supprimé.");
        if (!File.Exists(log.FilePath))
            throw new NotFoundException(nameof(AccountingExportLog), logId);

        var content = await File.ReadAllBytesAsync(log.FilePath, ct);

        if (log.Status == AccountingExportStatus.GENERATED)
        {
            log.Status = AccountingExportStatus.DOWNLOADED;
            log.DownloadedAt = _clock.UtcNow;
            await _audit.LogAsync(AuditAction.OPEN, nameof(AccountingExportLog), log.Id,
                $"Téléchargement de l'export {log.ExportNumber}", ct: ct);
            await _db.SaveChangesAsync(ct);
        }

        return new AccountingExportResult(content, log.ContentType, log.FileName);
    }
}
