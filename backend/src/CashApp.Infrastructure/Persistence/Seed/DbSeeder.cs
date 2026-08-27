using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Admin@123";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        await SeedAdminAsync(db, hasher, ct);
        await SeedAppSettingsAsync(db, ct);
        await SeedFeatureSettingsAsync(db, ct);
        await SeedCategoryGroupsAsync(db, ct);
        await db.SaveChangesAsync(ct); // persiste les groupes avant de résoudre leurs Id ci-dessous
        await SeedCategoriesAsync(db, ct);
        await BackfillCategoryGroupsAsync(db, ct);
        await SeedAccountingSettingsAsync(db, ct);

        await db.SaveChangesAsync(ct);
    }

    // Ligne singleton de configuration comptable, non configurée par défaut.
    private static async Task SeedAccountingSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.AccountingSettings.AnyAsync(ct)) return;

        db.AccountingSettings.Add(new Domain.Entities.V2.AccountingSettings
        {
            GenerationType = AccountingGenerationType.DETAILED,
            GenerationMode = AccountingGenerationMode.MANUAL,
            IsConfigured = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static async Task SeedAdminAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Username == DefaultAdminUsername, ct))
            return;

        db.Users.Add(new User
        {
            Username = DefaultAdminUsername,
            FullName = "Administrator",
            PasswordHash = hasher.Hash(DefaultAdminPassword),
            RoleCode = RoleCodes.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static async Task SeedAppSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        var defaults = new Dictionary<string, string>
        {
            { SettingKeys.AppMode, AppMode.ESSENTIAL.ToString() },
            { SettingKeys.DefaultCurrency, "XOF" },
            { SettingKeys.AutoBackupEnabled, "false" },
            { SettingKeys.AutoBackupTime, "23:00" },
            { SettingKeys.AutoBackupOnSessionClose, "false" },
            { SettingKeys.AllowOperationEditBeforeSessionClose, "true" },
            { SettingKeys.AllowSupervisorCloseAnySession, "true" },
            { SettingKeys.OperationRefPrefix, "OP" },
            { SettingKeys.BackupDirectory, "./backups" },
            // V2-A
            { SettingKeys.ApprovalRequiredForOperationAbove, "100000" },
            { SettingKeys.ApprovalRequiredForTransfer, "true" },
            { SettingKeys.ApprovalRequiredForBankDeposit, "true" },
            { SettingKeys.VarianceJustificationThreshold, "1000" },
            { SettingKeys.VarianceApprovalThreshold, "10000" },
            { SettingKeys.AutoCreateAnomalyOnLargeVariance, "true" },
            { SettingKeys.AutoCreateAnomalyOnRejectedTransfer, "true" },
            { SettingKeys.AutoCreateAnomalyOnRejectedDeposit, "true" },
            { SettingKeys.ApprovalDefaultApproverRole, RoleCodes.Supervisor },
            // V2-B
            { SettingKeys.AllowedAttachmentExtensions, "pdf,jpg,jpeg,png,xlsx,csv,docx" },
            { SettingKeys.MaxAttachmentSizeMb, "10" },
            { SettingKeys.AttachmentsRootPath, "./attachments" },
            { SettingKeys.ImportAllowPartialSuccess, "true" },
            { SettingKeys.ImportDefaultDateFormat, "yyyy-MM-dd" },
            { SettingKeys.ImportDefaultDecimalSeparator, "." },
            { SettingKeys.ImportsRootPath, "./imports" },
            // V3 — Comptabilité
            { SettingKeys.AccountingExportsRootPath, "./exports/accounting" }
        };

        var existing = await db.AppSettings.Select(s => s.SettingKey).ToListAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var (key, value) in defaults)
        {
            if (existing.Contains(key)) continue;
            db.AppSettings.Add(new AppSetting
            {
                SettingKey = key,
                SettingValue = value,
                CreatedAt = now
            });
        }
    }

    private static async Task SeedFeatureSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        var defs = new (string Code, string Name, bool Enabled)[]
        {
            // V1 — all enabled
            (FeatureCodes.CoreAuth,          "Authentification",       true),
            (FeatureCodes.CoreUsers,         "Utilisateurs",           true),
            (FeatureCodes.CoreCashRegisters, "Caisses",                true),
            (FeatureCodes.CoreCashSessions,  "Sessions de caisse",     true),
            (FeatureCodes.CoreOperations,    "Opérations",             true),
            (FeatureCodes.CoreCategories,    "Catégories",             true),
            (FeatureCodes.CoreDashboard,     "Tableau de bord",        true),
            (FeatureCodes.CoreExports,       "Exports",                true),
            (FeatureCodes.CoreBackup,        "Sauvegardes",            true),
            (FeatureCodes.CoreSettings,      "Paramètres",             true),

            // V2 — all disabled by default
            (FeatureCodes.AdvValidation,        "Workflow de validation",   false),
            (FeatureCodes.AdvTransfers,         "Transferts inter-caisses", false),
            (FeatureCodes.AdvBankDeposits,      "Dépôts banque",            false),
            (FeatureCodes.AdvAttachments,       "Pièces jointes",           false),
            (FeatureCodes.AdvAdvancedReports,   "Rapports avancés",         false),
            (FeatureCodes.AdvImports,           "Imports Excel/CSV",        false),
            (FeatureCodes.AdvReconciliation,    "Rapprochement",            false),
            (FeatureCodes.AdvVarianceManagement,"Gestion des écarts",       false),
            (FeatureCodes.AdvAnomalies,         "Anomalies",                false),
            (FeatureCodes.AdvAuditLog,          "Journal d'audit",          false),

            // V3 — Comptabilité
            (FeatureCodes.AdvAccounting,        "Comptabilité",             false)
        };

        var existing = await db.FeatureSettings.Select(f => f.FeatureCode).ToListAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var (code, name, enabled) in defs)
        {
            if (existing.Contains(code)) continue;
            db.FeatureSettings.Add(new FeatureSetting
            {
                FeatureCode = code,
                FeatureName = name,
                IsEnabled = enabled,
                CreatedAt = now
            });
        }
    }

    // Groupes par défaut proposés à l'organisation des catégories, et mapping des catégories
    // par défaut (seed ou créées dynamiquement par les modules Transferts/Dépôt banque) vers
    // leur groupe logique. Tout code non mappé retombe sur "Autres".
    private static readonly (string Name, string[] CategoryCodes)[] DefaultGroups =
    {
        ("Ventes & Encaissements", new[] { "SALE", "ADVANCE" }),
        ("Achats & Dépenses",      new[] { "PURCHASE", "TRANSPORT", "FUEL", "OFFICE_SUPPLIES" }),
        ("Transferts internes",    new[] { "TRANSFER", "TRANSFER_IN" }),
        ("Banque",                 new[] { "BANK_DEPOSIT" }),
        ("Autres",                 Array.Empty<string>())
    };
    private const string FallbackGroupName = "Autres";

    private static async Task SeedCategoryGroupsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.CategoryGroups.Select(g => g.Name).ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var (name, _) in DefaultGroups)
        {
            if (existing.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
            db.CategoryGroups.Add(new CategoryGroup { Name = name, IsActive = true, CreatedAt = now });
        }
    }

    private static async Task SeedCategoriesAsync(AppDbContext db, CancellationToken ct)
    {
        var defaults = new (string Code, string Label, OperationDirection Direction)[]
        {
            ("SALE",            "Vente",                       OperationDirection.IN),
            ("ADVANCE",         "Avance reçue",                OperationDirection.IN),
            ("PURCHASE",        "Achat",                       OperationDirection.OUT),
            ("TRANSPORT",       "Transport",                   OperationDirection.OUT),
            ("FUEL",            "Carburant",                   OperationDirection.OUT),
            ("OFFICE_SUPPLIES", "Fournitures de bureau",       OperationDirection.OUT)
        };

        var existing = await db.Categories.Select(c => c.Code).ToListAsync(ct);
        var groupsByCode = await BuildGroupIdByCategoryCodeAsync(db, ct);
        var now = DateTime.UtcNow;

        foreach (var (code, label, direction) in defaults)
        {
            if (existing.Contains(code)) continue;
            db.Categories.Add(new Category
            {
                Code = code,
                Label = label,
                Direction = direction,
                IsActive = true,
                CreatedAt = now,
                GroupId = groupsByCode.GetValueOrDefault(code)
            });
        }
    }

    // Rattrape les catégories déjà existantes en base (créées avant l'introduction des groupes,
    // via seed antérieur ou création dynamique par CashTransferService/BankDepositService) qui
    // n'ont pas encore de GroupId.
    private static async Task BackfillCategoryGroupsAsync(AppDbContext db, CancellationToken ct)
    {
        var orphans = await db.Categories.Where(c => c.GroupId == null).ToListAsync(ct);
        if (orphans.Count == 0) return;

        var groupsByCode = await BuildGroupIdByCategoryCodeAsync(db, ct);
        var fallbackId = await db.CategoryGroups
            .Where(g => g.Name == FallbackGroupName)
            .Select(g => (int?)g.Id).FirstOrDefaultAsync(ct);

        foreach (var c in orphans)
            c.GroupId = groupsByCode.TryGetValue(c.Code, out var gid) ? gid : fallbackId;
    }

    private static async Task<Dictionary<string, int>> BuildGroupIdByCategoryCodeAsync(AppDbContext db, CancellationToken ct)
    {
        var groupIdsByName = await db.CategoryGroups.ToDictionaryAsync(g => g.Name, g => g.Id, StringComparer.OrdinalIgnoreCase, ct);
        var map = new Dictionary<string, int>();
        foreach (var (name, codes) in DefaultGroups)
        {
            if (!groupIdsByName.TryGetValue(name, out var groupId)) continue;
            foreach (var code in codes) map[code] = groupId;
        }
        return map;
    }
}
