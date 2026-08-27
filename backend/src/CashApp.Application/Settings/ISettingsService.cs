using CashApp.Application.Settings.Dtos;
using CashApp.Domain.Enums;

namespace CashApp.Application.Settings;

public interface ISettingsService
{
    Task<GeneralSettingsDto> GetGeneralAsync(CancellationToken ct = default);
    Task<GeneralSettingsDto> UpdateGeneralAsync(GeneralSettingsDto dto, CancellationToken ct = default);

    Task<AppModeDto> GetAppModeAsync(CancellationToken ct = default);
    Task<AppModeDto> UpdateAppModeAsync(UpdateAppModeDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<FeatureSettingDto>> GetFeaturesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FeatureSettingDto>> UpdateFeaturesAsync(UpdateFeatureSettingsDto dto, CancellationToken ct = default);

    Task<string?> GetRawAsync(string key, CancellationToken ct = default);
    Task SetRawAsync(string key, string? value, CancellationToken ct = default);

    Task<CompanyInfoDto> GetCompanyAsync(CancellationToken ct = default);
    Task<CompanyInfoDto> UpdateCompanyAsync(CompanyInfoDto dto, CancellationToken ct = default);
}
