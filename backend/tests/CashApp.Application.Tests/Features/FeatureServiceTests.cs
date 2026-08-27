using CashApp.Application.Common.Exceptions;
using CashApp.Application.Features;
using CashApp.Application.Tests.Fakes;
using CashApp.Application.Tests.Infrastructure;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CashApp.Application.Tests.Features;

public class FeatureServiceTests
{
    [Fact]
    public async Task IsEnabledAsync_returns_true_for_enabled_feature()
    {
        var (db, clock) = TestDbContextFactory.Create();
        db.FeatureSettings.Add(new FeatureSetting { FeatureCode = FeatureCodes.CoreUsers, FeatureName = "Users", IsEnabled = true });
        await db.SaveChangesAsync();
        var svc = new FeatureService(db, clock);

        (await svc.IsEnabledAsync(FeatureCodes.CoreUsers)).Should().BeTrue();
    }

    [Fact]
    public async Task IsEnabledAsync_returns_false_for_disabled_or_unknown()
    {
        var (db, clock) = TestDbContextFactory.Create();
        db.FeatureSettings.Add(new FeatureSetting { FeatureCode = FeatureCodes.AdvImports, FeatureName = "Imports", IsEnabled = false });
        await db.SaveChangesAsync();
        var svc = new FeatureService(db, clock);

        (await svc.IsEnabledAsync(FeatureCodes.AdvImports)).Should().BeFalse();
        (await svc.IsEnabledAsync("UNKNOWN_CODE")).Should().BeFalse();
    }

    [Fact]
    public async Task EnsureEnabledAsync_throws_ForbiddenException_when_disabled()
    {
        var (db, clock) = TestDbContextFactory.Create();
        db.FeatureSettings.Add(new FeatureSetting { FeatureCode = FeatureCodes.AdvImports, FeatureName = "Imports", IsEnabled = false });
        await db.SaveChangesAsync();
        var svc = new FeatureService(db, clock);

        await FluentActions.Awaiting(() => svc.EnsureEnabledAsync(FeatureCodes.AdvImports))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SetEnabledAsync_updates_value_and_audit_timestamp()
    {
        var (db, clock) = TestDbContextFactory.Create();
        db.FeatureSettings.Add(new FeatureSetting { FeatureCode = FeatureCodes.CoreUsers, FeatureName = "Users", IsEnabled = false });
        await db.SaveChangesAsync();
        var svc = new FeatureService(db, clock);

        await svc.SetEnabledAsync(FeatureCodes.CoreUsers, true);

        var stored = db.FeatureSettings.Single();
        stored.IsEnabled.Should().BeTrue();
        stored.UpdatedAt.Should().Be(clock.UtcNow);
    }
}
