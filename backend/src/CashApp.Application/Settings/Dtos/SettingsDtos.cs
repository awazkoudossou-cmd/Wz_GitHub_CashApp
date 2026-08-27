using CashApp.Domain.Enums;

namespace CashApp.Application.Settings.Dtos;

public record AppModeDto(AppMode Mode);

public record UpdateAppModeDto(AppMode Mode);

public record FeatureSettingDto(
    int Id,
    string FeatureCode,
    string FeatureName,
    bool IsEnabled,
    DateTime? UpdatedAt);

public record UpdateFeatureSettingsDto(IReadOnlyList<FeatureToggleDto> Features);

public record FeatureToggleDto(string FeatureCode, bool IsEnabled);

public record CompanyInfoDto(
    string? Name,
    string? LegalForm,
    string? Address,
    string? City,
    string? Country,
    string? Phone,
    string? Email,
    string? Website,
    string? RegistrationNumber,
    string? TaxId,
    string? LogoPath);

public record GeneralSettingsDto(
    string DefaultCurrency,
    bool AutoBackupEnabled,
    string AutoBackupTime,
    bool AutoBackupOnSessionClose,
    bool AllowOperationEditBeforeSessionClose,
    bool AllowSupervisorCloseAnySession,
    string OperationRefPrefix,
    string BackupDirectory,
    string OpeningBalanceDefaultMode,               // ZERO | LAST_CLOSING_PHYSICAL
    decimal VarianceJustificationThreshold,         // au-dessus : justification obligatoire
    bool VarianceForceJustificationBelowThreshold,  // si true, force justification même en dessous
    bool VarianceTrackAllNonZero,                   // true = créer un VarianceCase pour tout écart non nul
    bool ShowDeletedOperationsInList,               // true = afficher les ops annulées/rejetées dans le menu Opérations
    int ReceiptCopiesCount);                        // 1 ou 2
