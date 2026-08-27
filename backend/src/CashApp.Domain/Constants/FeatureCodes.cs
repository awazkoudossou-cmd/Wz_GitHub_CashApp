namespace CashApp.Domain.Constants;

public static class FeatureCodes
{
    // V1 — Core modules
    public const string CoreAuth = "CORE_AUTH";
    public const string CoreUsers = "CORE_USERS";
    public const string CoreCashRegisters = "CORE_CASH_REGISTERS";
    public const string CoreCashSessions = "CORE_CASH_SESSIONS";
    public const string CoreOperations = "CORE_OPERATIONS";
    public const string CoreCategories = "CORE_CATEGORIES";
    public const string CoreDashboard = "CORE_DASHBOARD";
    public const string CoreExports = "CORE_EXPORTS";
    public const string CoreBackup = "CORE_BACKUP";
    public const string CoreSettings = "CORE_SETTINGS";

    // V2 — Advanced modules
    public const string AdvValidation = "ADV_VALIDATION";
    public const string AdvTransfers = "ADV_TRANSFERS";
    public const string AdvBankDeposits = "ADV_BANK_DEPOSITS";
    public const string AdvAttachments = "ADV_ATTACHMENTS";
    public const string AdvAdvancedReports = "ADV_ADVANCED_REPORTS";
    public const string AdvImports = "ADV_IMPORTS";
    public const string AdvReconciliation = "ADV_RECONCILIATION";
    public const string AdvVarianceManagement = "ADV_VARIANCE_MANAGEMENT";
    public const string AdvAnomalies = "ADV_ANOMALIES";
    public const string AdvAuditLog = "ADV_AUDIT_LOG";

    // V3 — Comptabilité
    public const string AdvAccounting = "ADV_ACCOUNTING";

    public static readonly IReadOnlyList<string> CoreAll = new[]
    {
        CoreAuth, CoreUsers, CoreCashRegisters, CoreCashSessions, CoreOperations,
        CoreCategories, CoreDashboard, CoreExports, CoreBackup, CoreSettings
    };

    public static readonly IReadOnlyList<string> AdvancedAll = new[]
    {
        AdvValidation, AdvTransfers, AdvBankDeposits, AdvAttachments, AdvAdvancedReports,
        AdvImports, AdvReconciliation, AdvVarianceManagement, AdvAnomalies, AdvAuditLog,
        AdvAccounting
    };
}
