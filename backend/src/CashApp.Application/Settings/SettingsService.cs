using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings.Dtos;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Settings;

public class SettingsService : ISettingsService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SettingsService(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<GeneralSettingsDto> GetGeneralAsync(CancellationToken ct = default)
    {
        var dict = await LoadSettingsAsync(ct);
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        decimal.TryParse(Get(dict, SettingKeys.VarianceJustificationThreshold, "0"),
            System.Globalization.NumberStyles.Number, ci, out var justifyThr);
        return new GeneralSettingsDto(
            DefaultCurrency: Get(dict, SettingKeys.DefaultCurrency, "XOF"),
            AutoBackupEnabled: ParseBool(Get(dict, SettingKeys.AutoBackupEnabled, "false")),
            AutoBackupTime: Get(dict, SettingKeys.AutoBackupTime, "23:00"),
            AutoBackupOnSessionClose: ParseBool(Get(dict, SettingKeys.AutoBackupOnSessionClose, "false")),
            AllowOperationEditBeforeSessionClose: ParseBool(Get(dict, SettingKeys.AllowOperationEditBeforeSessionClose, "true")),
            AllowSupervisorCloseAnySession: ParseBool(Get(dict, SettingKeys.AllowSupervisorCloseAnySession, "true")),
            OperationRefPrefix: Get(dict, SettingKeys.OperationRefPrefix, "OP"),
            BackupDirectory: Get(dict, SettingKeys.BackupDirectory, "./backups"),
            OpeningBalanceDefaultMode: Get(dict, SettingKeys.OpeningBalanceDefaultMode, "ZERO"),
            VarianceJustificationThreshold: justifyThr,
            VarianceForceJustificationBelowThreshold: ParseBool(Get(dict, SettingKeys.VarianceForceJustificationBelowThreshold, "false")),
            VarianceTrackAllNonZero: ParseBool(Get(dict, SettingKeys.VarianceTrackAllNonZero, "false")),
            ShowDeletedOperationsInList: ParseBool(Get(dict, SettingKeys.ShowDeletedOperationsInList, "false")),
            ReceiptCopiesCount: int.TryParse(Get(dict, SettingKeys.ReceiptCopiesCount, "1"), out var rc) && rc == 2 ? 2 : 1);
    }

    public async Task<GeneralSettingsDto> UpdateGeneralAsync(GeneralSettingsDto dto, CancellationToken ct = default)
    {
        await UpsertAsync(SettingKeys.DefaultCurrency, dto.DefaultCurrency, ct);
        await UpsertAsync(SettingKeys.AutoBackupEnabled, dto.AutoBackupEnabled.ToString().ToLowerInvariant(), ct);
        await UpsertAsync(SettingKeys.AutoBackupTime, dto.AutoBackupTime, ct);
        await UpsertAsync(SettingKeys.AutoBackupOnSessionClose, dto.AutoBackupOnSessionClose.ToString().ToLowerInvariant(), ct);
        await UpsertAsync(SettingKeys.AllowOperationEditBeforeSessionClose, dto.AllowOperationEditBeforeSessionClose.ToString().ToLowerInvariant(), ct);
        await UpsertAsync(SettingKeys.AllowSupervisorCloseAnySession, dto.AllowSupervisorCloseAnySession.ToString().ToLowerInvariant(), ct);
        await UpsertAsync(SettingKeys.OperationRefPrefix, dto.OperationRefPrefix, ct);
        await UpsertAsync(SettingKeys.BackupDirectory, dto.BackupDirectory, ct);
        await UpsertAsync(SettingKeys.OpeningBalanceDefaultMode,
            string.Equals(dto.OpeningBalanceDefaultMode, "LAST_CLOSING_PHYSICAL", StringComparison.OrdinalIgnoreCase)
                ? "LAST_CLOSING_PHYSICAL" : "ZERO", ct);
        await UpsertAsync(SettingKeys.VarianceJustificationThreshold,
            dto.VarianceJustificationThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture), ct);
        await UpsertAsync(SettingKeys.VarianceForceJustificationBelowThreshold,
            dto.VarianceForceJustificationBelowThreshold.ToString().ToLowerInvariant(), ct);
        await UpsertAsync(SettingKeys.VarianceTrackAllNonZero,
            dto.VarianceTrackAllNonZero.ToString().ToLowerInvariant(), ct);
        await UpsertAsync(SettingKeys.ShowDeletedOperationsInList,
            dto.ShowDeletedOperationsInList.ToString().ToLowerInvariant(), ct);
        await UpsertAsync(SettingKeys.ReceiptCopiesCount,
            (dto.ReceiptCopiesCount == 2 ? 2 : 1).ToString(System.Globalization.CultureInfo.InvariantCulture), ct);

        await _db.SaveChangesAsync(ct);
        return await GetGeneralAsync(ct);
    }

    public async Task<AppModeDto> GetAppModeAsync(CancellationToken ct = default)
    {
        var raw = await GetRawAsync(SettingKeys.AppMode, ct) ?? AppMode.ESSENTIAL.ToString();
        return new AppModeDto(Enum.TryParse<AppMode>(raw, out var m) ? m : AppMode.ESSENTIAL);
    }

    public async Task<AppModeDto> UpdateAppModeAsync(UpdateAppModeDto dto, CancellationToken ct = default)
    {
        await UpsertAsync(SettingKeys.AppMode, dto.Mode.ToString(), ct);

        // Désactive automatiquement toutes les features ADV_* qui ne sont pas autorisées dans ce mode.
        // Source de vérité unique : si tu changes le mode, les modules avancés incompatibles tombent à OFF.
        var allowed = AppModeFeatures.AllowedAdvFor(dto.Mode);
        var advFeatures = await _db.FeatureSettings
            .Where(f => f.FeatureCode.StartsWith("ADV_"))
            .ToListAsync(ct);
        var now = _clock.UtcNow;
        foreach (var f in advFeatures)
        {
            if (!allowed.Contains(f.FeatureCode) && f.IsEnabled)
            {
                f.IsEnabled = false;
                f.UpdatedBy = _currentUser.UserId;
                f.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new AppModeDto(dto.Mode);
    }

    public async Task<IReadOnlyList<FeatureSettingDto>> GetFeaturesAsync(CancellationToken ct = default)
    {
        return await _db.FeatureSettings
            .AsNoTracking()
            .OrderBy(f => f.FeatureCode)
            .Select(f => new FeatureSettingDto(f.Id, f.FeatureCode, f.FeatureName, f.IsEnabled, f.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FeatureSettingDto>> UpdateFeaturesAsync(UpdateFeatureSettingsDto dto, CancellationToken ct = default)
    {
        // Récupère le mode courant pour borner ce que l'utilisateur peut activer.
        var appMode = (await GetAppModeAsync(ct)).Mode;
        var allowedAdv = AppModeFeatures.AllowedAdvFor(appMode);

        var codes = dto.Features.Select(f => f.FeatureCode).ToList();
        var features = await _db.FeatureSettings
            .Where(f => codes.Contains(f.FeatureCode))
            .ToListAsync(ct);

        var now = _clock.UtcNow;
        foreach (var toggle in dto.Features)
        {
            var f = features.FirstOrDefault(x => x.FeatureCode == toggle.FeatureCode);
            if (f is null) continue;

            // Sécurité : si la feature est avancée et non autorisée par le mode courant,
            // on l'ignore (et on force OFF). Empêche d'activer ADV_TRANSFERS en mode ESSENTIAL.
            var desired = toggle.IsEnabled;
            if (f.FeatureCode.StartsWith("ADV_") && !allowedAdv.Contains(f.FeatureCode))
                desired = false;

            f.IsEnabled = desired;
            f.UpdatedBy = _currentUser.UserId;
            f.UpdatedAt = now;
        }
        await _db.SaveChangesAsync(ct);
        return await GetFeaturesAsync(ct);
    }

    public async Task<string?> GetRawAsync(string key, CancellationToken ct = default)
    {
        var s = await _db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.SettingKey == key, ct);
        return s?.SettingValue;
    }

    public async Task SetRawAsync(string key, string? value, CancellationToken ct = default)
    {
        await UpsertAsync(key, value, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CompanyInfoDto> GetCompanyAsync(CancellationToken ct = default)
    {
        var dict = await LoadSettingsAsync(ct);
        string? get(string k) => dict.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;
        return new CompanyInfoDto(
            Name: get(SettingKeys.CompanyName),
            LegalForm: get(SettingKeys.CompanyLegalForm),
            Address: get(SettingKeys.CompanyAddress),
            City: get(SettingKeys.CompanyCity),
            Country: get(SettingKeys.CompanyCountry),
            Phone: get(SettingKeys.CompanyPhone),
            Email: get(SettingKeys.CompanyEmail),
            Website: get(SettingKeys.CompanyWebsite),
            RegistrationNumber: get(SettingKeys.CompanyRegistrationNumber),
            TaxId: get(SettingKeys.CompanyTaxId),
            LogoPath: get(SettingKeys.CompanyLogoPath));
    }

    public async Task<CompanyInfoDto> UpdateCompanyAsync(CompanyInfoDto dto, CancellationToken ct = default)
    {
        await UpsertAsync(SettingKeys.CompanyName, dto.Name?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyLegalForm, dto.LegalForm?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyAddress, dto.Address?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyCity, dto.City?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyCountry, dto.Country?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyPhone, dto.Phone?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyEmail, dto.Email?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyWebsite, dto.Website?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyRegistrationNumber, dto.RegistrationNumber?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyTaxId, dto.TaxId?.Trim(), ct);
        await UpsertAsync(SettingKeys.CompanyLogoPath, dto.LogoPath?.Trim(), ct);
        await _db.SaveChangesAsync(ct);
        return await GetCompanyAsync(ct);
    }

    private async Task<Dictionary<string, string?>> LoadSettingsAsync(CancellationToken ct)
    {
        return await _db.AppSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue, ct);
    }

    private async Task UpsertAsync(string key, string? value, CancellationToken ct)
    {
        var existing = await _db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == key, ct);
        if (existing is null)
        {
            _db.AppSettings.Add(new AppSetting
            {
                SettingKey = key,
                SettingValue = value,
                UpdatedBy = _currentUser.UserId
            });
        }
        else
        {
            existing.SettingValue = value;
            existing.UpdatedBy = _currentUser.UserId;
            existing.UpdatedAt = _clock.UtcNow;
        }
    }

    private static string Get(Dictionary<string, string?> dict, string key, string fallback) =>
        dict.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v! : fallback;

    private static bool ParseBool(string s) =>
        bool.TryParse(s, out var b) && b;
}
