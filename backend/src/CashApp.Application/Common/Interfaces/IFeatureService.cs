namespace CashApp.Application.Common.Interfaces;

public interface IFeatureService
{
    Task<bool> IsEnabledAsync(string featureCode, CancellationToken ct = default);
    Task EnsureEnabledAsync(string featureCode, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, bool>> GetAllAsync(CancellationToken ct = default);
    Task SetEnabledAsync(string featureCode, bool enabled, CancellationToken ct = default);
}
