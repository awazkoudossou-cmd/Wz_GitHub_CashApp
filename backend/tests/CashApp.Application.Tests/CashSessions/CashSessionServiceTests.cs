using CashApp.Application.CashSessions;
using CashApp.Application.CashSessions.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Settings;
using CashApp.Application.Tests.Fakes;
using CashApp.Application.Tests.Infrastructure;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using CashApp.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CashApp.Application.Tests.CashSessions;

public class CashSessionServiceTests
{
    private static CashSessionService BuildService(AppDbContext db, FakeClock clock, ISettingsService? settings = null, FakeCurrentUser? user = null,
        CashApp.Application.Common.Interfaces.IFeatureService? features = null,
        CashApp.Application.Accounting.IAccountingQueueService? accountingQueue = null)
    {
        settings ??= Substitute.For<ISettingsService>();
        user ??= new FakeCurrentUser();
        if (features is null)
        {
            features = Substitute.For<CashApp.Application.Common.Interfaces.IFeatureService>();
            features.IsEnabledAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        }
        var variance = Substitute.For<CashApp.Application.Variances.IVarianceService>();
        var anomaly = Substitute.For<CashApp.Application.Anomalies.IAnomalyService>();
        var approval = Substitute.For<CashApp.Application.Approvals.IApprovalService>();
        var audit = Substitute.For<CashApp.Application.Common.Interfaces.IAuditLogger>();
        var access = Substitute.For<CashApp.Application.Common.Interfaces.ICashRegisterAccessService>();
        access.GetAccessibleRegisterIdsAsync(Arg.Any<CancellationToken>()).Returns(new[] { 10 });
        access.CanAccessAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        accountingQueue ??= Substitute.For<CashApp.Application.Accounting.IAccountingQueueService>();
        return new CashSessionService(db, user, clock, settings, features, variance, anomaly, approval, audit, access, accountingQueue);
    }

    [Fact]
    public async Task OpenAsync_creates_OPEN_session_with_opening_balance()
    {
        var (db, clock) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        var svc = BuildService(db, clock);

        var dto = new OpenCashSessionDto(CashRegisterId: 10, OpeningBalance: 5000m, OpenComment: "fond initial");
        var result = await svc.OpenAsync(dto);

        result.Status.Should().Be(CashSessionStatus.OPEN);
        result.OpeningBalance.Should().Be(5000m);
        result.TheoreticalBalance.Should().Be(5000m);
        db.CashSessions.Single().OpenedBy.Should().Be(1);
    }

    [Fact]
    public async Task OpenAsync_throws_when_existing_OPEN_session_for_same_register()
    {
        var (db, clock) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        db.CashSessions.Add(new CashSession
        {
            CashRegisterId = 10, OpenedBy = 1, OpenedAt = clock.UtcNow, OpeningBalance = 0,
            Status = CashSessionStatus.OPEN
        });
        await db.SaveChangesAsync();
        var svc = BuildService(db, clock);

        await FluentActions.Awaiting(() => svc.OpenAsync(new OpenCashSessionDto(10, 0, null)))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "CASH_SESSION_ALREADY_OPEN");
    }

    [Fact]
    public async Task OpenAsync_throws_when_register_inactive()
    {
        var (db, clock) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        var register = db.CashRegisters.Single();
        register.IsActive = false;
        await db.SaveChangesAsync();
        var svc = BuildService(db, clock);

        await FluentActions.Awaiting(() => svc.OpenAsync(new OpenCashSessionDto(10, 0, null)))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "CASH_REGISTER_INACTIVE");
    }

    [Fact]
    public async Task RecomputeTheoreticalBalance_returns_opening_plus_in_minus_out()
    {
        var (db, clock) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        var session = new CashSession
        {
            CashRegisterId = 10, OpenedBy = 1, OpenedAt = clock.UtcNow,
            OpeningBalance = 1000, Status = CashSessionStatus.OPEN
        };
        db.CashSessions.Add(session);
        await db.SaveChangesAsync();

        db.CashOperations.AddRange(
            new CashOperation { OperationRef = "R-1", CashRegisterId = 10, CashSessionId = session.Id,
                OperationDate = clock.UtcNow, Direction = OperationDirection.IN, CategoryId = 100,
                Amount = 500, CurrencyCode = "XOF", PaymentMethod = PaymentMethod.CASH, Label = "v1" },
            new CashOperation { OperationRef = "R-2", CashRegisterId = 10, CashSessionId = session.Id,
                OperationDate = clock.UtcNow, Direction = OperationDirection.OUT, CategoryId = 101,
                Amount = 200, CurrencyCode = "XOF", PaymentMethod = PaymentMethod.CASH, Label = "a1" },
            new CashOperation { OperationRef = "R-3", CashRegisterId = 10, CashSessionId = session.Id,
                OperationDate = clock.UtcNow, Direction = OperationDirection.IN, CategoryId = 100,
                Amount = 999, CurrencyCode = "XOF", PaymentMethod = PaymentMethod.CASH, Label = "annulée",
                IsDeleted = true }
        );
        await db.SaveChangesAsync();
        var svc = BuildService(db, clock);

        var theoretical = await svc.RecomputeTheoreticalBalanceAsync(session.Id);

        // Opérations supprimées ignorées : 1000 + 500 - 200 = 1300
        theoretical.Should().Be(1300m);
    }

    [Fact]
    public async Task CloseAsync_computes_variance_and_locks_session()
    {
        var (db, clock) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);

        var session = new CashSession
        {
            CashRegisterId = 10, OpenedBy = 1, OpenedAt = clock.UtcNow,
            OpeningBalance = 1000, Status = CashSessionStatus.OPEN
        };
        db.CashSessions.Add(session);
        await db.SaveChangesAsync();

        var settings = Substitute.For<ISettingsService>();
        settings.GetRawAsync(SettingKeys.AllowSupervisorCloseAnySession, Arg.Any<CancellationToken>())
            .Returns("true");

        var svc = BuildService(db, clock, settings);

        var result = await svc.CloseAsync(session.Id, new CloseCashSessionDto(PhysicalBalance: 950m, CloseComment: "écart -50"));

        result.Status.Should().Be(CashSessionStatus.CLOSED);
        result.PhysicalBalance.Should().Be(950m);
        result.TheoreticalBalance.Should().Be(1000m);
        result.VarianceAmount.Should().Be(-50m);
        result.ClosedBy.Should().Be(1);
    }

    [Fact]
    public async Task CloseAsync_forbids_cashier_closing_someone_elses_session()
    {
        var (db, clock) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);

        db.Users.Add(new User { Id = 2, Username = "alice", FullName = "Alice",
            PasswordHash = "x", RoleCode = RoleCodes.Cashier, IsActive = true });
        var session = new CashSession
        {
            CashRegisterId = 10, OpenedBy = 2, OpenedAt = clock.UtcNow,
            OpeningBalance = 0, Status = CashSessionStatus.OPEN
        };
        db.CashSessions.Add(session);
        await db.SaveChangesAsync();

        var user = new FakeCurrentUser { UserId = 99 };
        user.SetRole(RoleCodes.Cashier);
        var settings = Substitute.For<ISettingsService>();
        settings.GetRawAsync(SettingKeys.AllowSupervisorCloseAnySession, Arg.Any<CancellationToken>()).Returns("false");
        var svc = BuildService(db, clock, settings, user);

        await FluentActions.Awaiting(() => svc.CloseAsync(session.Id, new CloseCashSessionDto(0, null)))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CloseAsync_enqueues_automatic_accounting_generation_when_feature_enabled()
    {
        var (db, clock) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        var session = new CashSession
        {
            CashRegisterId = 10, OpenedBy = 1, OpenedAt = clock.UtcNow,
            OpeningBalance = 0, Status = CashSessionStatus.OPEN
        };
        db.CashSessions.Add(session);
        await db.SaveChangesAsync();

        var features = Substitute.For<CashApp.Application.Common.Interfaces.IFeatureService>();
        features.IsEnabledAsync(CashApp.Domain.Constants.FeatureCodes.AdvAccounting, Arg.Any<CancellationToken>()).Returns(true);
        var accountingQueue = Substitute.For<CashApp.Application.Accounting.IAccountingQueueService>();
        var svc = BuildService(db, clock, features: features, accountingQueue: accountingQueue);

        await svc.CloseAsync(session.Id, new CloseCashSessionDto(0, null));

        await accountingQueue.Received(1).EnqueueAutomaticAsync(10, session.OpenedAt, Arg.Any<DateTime>(), 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseAsync_does_not_enqueue_when_accounting_feature_disabled()
    {
        var (db, clock) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        var session = new CashSession
        {
            CashRegisterId = 10, OpenedBy = 1, OpenedAt = clock.UtcNow,
            OpeningBalance = 0, Status = CashSessionStatus.OPEN
        };
        db.CashSessions.Add(session);
        await db.SaveChangesAsync();

        var accountingQueue = Substitute.For<CashApp.Application.Accounting.IAccountingQueueService>();
        var svc = BuildService(db, clock, accountingQueue: accountingQueue); // features par défaut : tout désactivé

        await svc.CloseAsync(session.Id, new CloseCashSessionDto(0, null));

        await accountingQueue.DidNotReceive().EnqueueAutomaticAsync(
            Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
