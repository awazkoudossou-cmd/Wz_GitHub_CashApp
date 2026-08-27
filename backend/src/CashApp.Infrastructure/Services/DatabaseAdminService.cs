using CashApp.Application.Admin;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Domain.Constants;
using CashApp.Infrastructure.Persistence;
using CashApp.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Infrastructure.Services;

public class DatabaseAdminService : IDatabaseAdminService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ISettingsService _settings;

    public DatabaseAdminService(AppDbContext db, IPasswordHasher hasher, ISettingsService settings)
    {
        _db = db;
        _hasher = hasher;
        _settings = settings;
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        // 1) Vide les répertoires utilisateur (attachments, imports). On le fait AVANT le drop
        //    parce que les chemins sont lus depuis les settings.
        await TryClearDirectoryFromSettingAsync(SettingKeys.AttachmentsRootPath, "./attachments", ct);
        await TryClearDirectoryFromSettingAsync(SettingKeys.ImportsRootPath, "./imports", ct);

        // 2) Drop + recréation du schéma.
        await _db.Database.EnsureDeletedAsync(ct);
        await _db.Database.EnsureCreatedAsync(ct);

        // 3) Reseed initial (admin / settings / features / catégories).
        await DbSeeder.SeedAsync(_db, _hasher, ct);
    }

    private async Task TryClearDirectoryFromSettingAsync(string key, string fallback, CancellationToken ct)
    {
        try
        {
            var path = await _settings.GetRawAsync(key, ct);
            var dir = Path.GetFullPath(string.IsNullOrWhiteSpace(path) ? fallback : path!);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // Best-effort : on n'empêche pas le reset si la suppression de disque échoue.
        }
    }
}
