using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _db;
    private readonly ISettingsService _settings;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public BackupService(AppDbContext db, ISettingsService settings, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _settings = settings;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<string> CreateBackupAsync(CancellationToken ct = default)
    {
        var dir = await ResolveBackupDirAsync(ct);
        Directory.CreateDirectory(dir);

        var dbPath = ResolveDbFilePath();
        if (!File.Exists(dbPath))
            throw new BusinessRuleException("BACKUP_DB_FILE_MISSING", $"Fichier SQLite introuvable : {dbPath}");

        var stamp = _clock.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"cashapp_{stamp}.db";
        var target = Path.Combine(dir, fileName);

        // VACUUM INTO produit un snapshot cohérent.
        var sql = $"VACUUM INTO '{target.Replace("'", "''")}'";
        await _db.Database.ExecuteSqlRawAsync(sql, ct);

        _db.BackupLogs.Add(new BackupLog
        {
            FileName = fileName,
            FilePath = target,
            CreatedBy = _currentUser.UserId
        });
        await _db.SaveChangesAsync(ct);

        return target;
    }

    public async Task RestoreBackupAsync(string backupFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(backupFilePath))
            throw new NotFoundException(nameof(BackupLog), backupFilePath);

        var dbPath = ResolveDbFilePath();

        // Sécurité : on ferme la connexion EF et on remplace le fichier.
        await _db.Database.CloseConnectionAsync();

        var safety = dbPath + ".pre-restore-" + _clock.UtcNow.ToString("yyyyMMddHHmmss");
        if (File.Exists(dbPath)) File.Copy(dbPath, safety, overwrite: true);

        File.Copy(backupFilePath, dbPath, overwrite: true);
    }

    public Task<IReadOnlyList<string>> ListBackupsAsync(CancellationToken ct = default)
    {
        return ListBackupsInternalAsync(ct);
    }

    private async Task<IReadOnlyList<string>> ListBackupsInternalAsync(CancellationToken ct)
    {
        var dir = await ResolveBackupDirAsync(ct);
        if (!Directory.Exists(dir)) return Array.Empty<string>();
        return Directory.GetFiles(dir, "cashapp_*.db")
            .OrderByDescending(f => f)
            .ToList();
    }

    private async Task<string> ResolveBackupDirAsync(CancellationToken ct)
    {
        var setting = await _settings.GetRawAsync(SettingKeys.BackupDirectory, ct);
        var dir = string.IsNullOrWhiteSpace(setting) ? "./backups" : setting!;
        return Path.GetFullPath(dir);
    }

    private string ResolveDbFilePath()
    {
        var conn = _db.Database.GetConnectionString() ?? string.Empty;
        // SQLite : "Data Source=cashapp.db"
        const string key = "Data Source=";
        var idx = conn.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) throw new InvalidOperationException("Impossible d'extraire le chemin SQLite.");
        var rest = conn[(idx + key.Length)..];
        var end = rest.IndexOf(';');
        var raw = (end >= 0 ? rest[..end] : rest).Trim();
        return Path.GetFullPath(raw);
    }
}
