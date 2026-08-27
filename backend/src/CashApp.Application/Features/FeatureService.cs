using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Features;

public class FeatureService : IFeatureService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public FeatureService(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<bool> IsEnabledAsync(string featureCode, CancellationToken ct = default)
    {
        var f = await _db.FeatureSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FeatureCode == featureCode, ct);
        return f?.IsEnabled ?? false;
    }

    public async Task EnsureEnabledAsync(string featureCode, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(featureCode, ct))
            throw new ForbiddenException($"Module '{featureCode}' désactivé.");
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.FeatureSettings
            .AsNoTracking()
            .ToDictionaryAsync(f => f.FeatureCode, f => f.IsEnabled, ct);
    }

    public async Task SetEnabledAsync(string featureCode, bool enabled, CancellationToken ct = default)
    {
        var f = await _db.FeatureSettings.FirstOrDefaultAsync(x => x.FeatureCode == featureCode, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.FeatureSetting), featureCode);

        f.IsEnabled = enabled;
        f.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
