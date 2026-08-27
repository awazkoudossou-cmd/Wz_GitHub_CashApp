using CashApp.Domain.Constants;
using CashApp.Domain.Enums;

namespace CashApp.Application.Settings;

// Mapping unique mode applicatif -> features avancées autorisées.
// Doit rester en miroir de la même table côté frontend (FeatureSettingsPage.ADV_BY_MODE).
internal static class AppModeFeatures
{
    public static IReadOnlySet<string> AllowedAdvFor(AppMode mode) => mode switch
    {
        AppMode.ESSENTIAL => Essential,
        AppMode.INTERMEDIATE => Intermediate,
        AppMode.ADVANCED => Advanced,
        _ => Essential
    };

    private static readonly HashSet<string> Essential = new();

    private static readonly HashSet<string> Intermediate = new()
    {
        FeatureCodes.AdvValidation,
        FeatureCodes.AdvVarianceManagement,
        FeatureCodes.AdvAuditLog
    };

    private static readonly HashSet<string> Advanced = new()
    {
        FeatureCodes.AdvValidation,
        FeatureCodes.AdvTransfers,
        FeatureCodes.AdvBankDeposits,
        FeatureCodes.AdvAttachments,
        FeatureCodes.AdvAdvancedReports,
        FeatureCodes.AdvImports,
        FeatureCodes.AdvReconciliation,
        FeatureCodes.AdvVarianceManagement,
        FeatureCodes.AdvAuditLog,
        FeatureCodes.AdvAccounting
        // ADV_ANOMALIES volontairement absent (module masqué).
    };
}
