namespace CashApp.Domain.Constants;

public static class SettingKeys
{
    public const string AppMode = "APP_MODE";
    public const string DefaultCurrency = "DEFAULT_CURRENCY";
    public const string AutoBackupEnabled = "AUTO_BACKUP_ENABLED";
    public const string AutoBackupTime = "AUTO_BACKUP_TIME";
    public const string AutoBackupOnSessionClose = "AUTO_BACKUP_ON_SESSION_CLOSE";
    public const string AllowOperationEditBeforeSessionClose = "ALLOW_OPERATION_EDIT_BEFORE_SESSION_CLOSE";
    public const string AllowSupervisorCloseAnySession = "ALLOW_SUPERVISOR_CLOSE_ANY_SESSION";
    public const string OperationRefPrefix = "OPERATION_REF_PREFIX";
    public const string BackupDirectory = "BACKUP_DIRECTORY";

    // V2-A
    public const string ApprovalRequiredForOperationAbove = "APPROVAL_REQUIRED_FOR_OPERATION_ABOVE";
    public const string ApprovalRequiredForTransfer = "APPROVAL_REQUIRED_FOR_TRANSFER";
    public const string ApprovalRequiredForBankDeposit = "APPROVAL_REQUIRED_FOR_BANK_DEPOSIT";
    public const string VarianceJustificationThreshold = "VARIANCE_JUSTIFICATION_THRESHOLD";
    public const string VarianceApprovalThreshold = "VARIANCE_APPROVAL_THRESHOLD";
    public const string AutoCreateAnomalyOnLargeVariance = "AUTO_CREATE_ANOMALY_ON_LARGE_VARIANCE";
    public const string AutoCreateAnomalyOnRejectedTransfer = "AUTO_CREATE_ANOMALY_ON_REJECTED_TRANSFER";
    public const string AutoCreateAnomalyOnRejectedDeposit = "AUTO_CREATE_ANOMALY_ON_REJECTED_DEPOSIT";
    public const string ApprovalDefaultApproverRole = "APPROVAL_DEFAULT_APPROVER_ROLE";

    // V1.x — UX par défaut
    public const string OpeningBalanceDefaultMode = "OPENING_BALANCE_DEFAULT_MODE";       // ZERO | LAST_CLOSING_PHYSICAL
    public const string VarianceForceJustificationBelowThreshold = "VARIANCE_FORCE_JUSTIFICATION_BELOW_THRESHOLD"; // bool : si false, écarts < VarianceJustificationThreshold n'exigent pas de justification
    public const string VarianceTrackAllNonZero = "VARIANCE_TRACK_ALL_NON_ZERO"; // bool : true = créer un VarianceCase pour tout écart non nul ; false = uniquement si AdvVarianceManagement actif + seuil de justification atteint
    public const string ShowDeletedOperationsInList = "SHOW_DELETED_OPERATIONS_IN_LIST"; // bool : true = afficher les ops soft-deleted (annulées/rejetées) dans le menu Opérations

    // Informations de l'entreprise
    public const string CompanyName = "COMPANY_NAME";
    public const string CompanyLegalForm = "COMPANY_LEGAL_FORM";
    public const string CompanyAddress = "COMPANY_ADDRESS";
    public const string CompanyCity = "COMPANY_CITY";
    public const string CompanyCountry = "COMPANY_COUNTRY";
    public const string CompanyPhone = "COMPANY_PHONE";
    public const string CompanyEmail = "COMPANY_EMAIL";
    public const string CompanyWebsite = "COMPANY_WEBSITE";
    public const string CompanyRegistrationNumber = "COMPANY_REGISTRATION_NUMBER";  // RCCM / SIRET / etc.
    public const string CompanyTaxId = "COMPANY_TAX_ID";  // NIF / TVA
    public const string CompanyLogoPath = "COMPANY_LOGO_PATH";

    // Reçus PDF
    public const string ReceiptCopiesCount = "RECEIPT_COPIES_COUNT";    // "1" ou "2"

    // V2-B
    public const string AllowedAttachmentExtensions = "ALLOWED_ATTACHMENT_EXTENSIONS";
    public const string MaxAttachmentSizeMb = "MAX_ATTACHMENT_SIZE_MB";
    public const string AttachmentsRootPath = "ATTACHMENTS_ROOT_PATH";
    public const string ImportAllowPartialSuccess = "IMPORT_ALLOW_PARTIAL_SUCCESS";
    public const string ImportDefaultDateFormat = "IMPORT_DEFAULT_DATE_FORMAT";
    public const string ImportDefaultDecimalSeparator = "IMPORT_DEFAULT_DECIMAL_SEPARATOR";
    public const string ImportsRootPath = "IMPORTS_ROOT_PATH";

    // V3 — Comptabilité
    public const string AccountingExportsRootPath = "ACCOUNTING_EXPORTS_ROOT_PATH";
}
