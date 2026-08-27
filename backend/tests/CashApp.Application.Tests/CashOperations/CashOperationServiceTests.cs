using CashApp.Application.CashOperations;
using CashApp.Application.CashOperations.Dtos;
using CashApp.Application.CashSessions;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Application.Tests.Fakes;
using CashApp.Application.Tests.Infrastructure;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using CashApp.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CashApp.Application.Tests.CashOperations;

public class CashOperationServiceTests
{
    private static (CashOperationService Svc, AppDbContext Db, FakeClock Clock, CashSession Session) BuildContextAsync()
    {
        var (db, clock) = TestDbContextFactory.Create();
        TestDbContextFactory.SeedMinimalAsync(db).GetAwaiter().GetResult();
        var session = new CashSession
        {
            CashRegisterId = 10, OpenedBy = 1, OpenedAt = clock.UtcNow,
            OpeningBalance = 0, Status = CashSessionStatus.OPEN
        };
        db.CashSessions.Add(session);
        db.SaveChanges();

        var user = new FakeCurrentUser();
        var refGen = Substitute.For<IReferenceGeneratorService>();
        refGen.NextOperationRefAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"OP-{DateTime.UtcNow.Ticks}");
        var sessions = Substitute.For<ICashSessionService>();
        var settings = Substitute.For<ISettingsService>();
        var approval = Substitute.For<CashApp.Application.Approvals.IApprovalService>();
        var features = Substitute.For<CashApp.Application.Common.Interfaces.IFeatureService>();
        features.IsEnabledAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var audit = Substitute.For<CashApp.Application.Common.Interfaces.IAuditLogger>();
        var access = Substitute.For<CashApp.Application.Common.Interfaces.ICashRegisterAccessService>();
        access.GetAccessibleRegisterIdsAsync(Arg.Any<CancellationToken>()).Returns(new[] { 10 });
        access.CanAccessAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        var thirdParties = Substitute.For<CashApp.Application.ThirdParties.IThirdPartyService>();
        return (new CashOperationService(db, user, clock, refGen, sessions, settings, approval, features, audit, access, thirdParties), db, clock, session);
    }

    [Fact]
    public async Task CreateAsync_succeeds_when_direction_matches_category()
    {
        var (svc, db, clock, session) = BuildContextAsync();

        var dto = new CreateCashOperationDto(
            CashSessionId: session.Id,
            OperationDate: clock.UtcNow,
            Direction: OperationDirection.IN,
            CategoryId: 100, // SALE / IN
            Amount: 1500m,
            PaymentMethod: PaymentMethod.CASH,
            Label: "Vente du matin",
            Description: null, ExternalReference: null, ThirdPartyName: null);

        var result = await svc.CreateAsync(dto);

        result.Direction.Should().Be(OperationDirection.IN);
        result.Amount.Should().Be(1500m);
        db.CashOperations.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_rejects_direction_category_mismatch()
    {
        var (svc, _, clock, session) = BuildContextAsync();
        var dto = new CreateCashOperationDto(
            session.Id, clock.UtcNow,
            Direction: OperationDirection.IN,
            CategoryId: 101, // PURCHASE / OUT
            Amount: 100, PaymentMethod: PaymentMethod.CASH,
            Label: "x", Description: null, ExternalReference: null, ThirdPartyName: null);

        await FluentActions.Awaiting(() => svc.CreateAsync(dto))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "CATEGORY_DIRECTION_MISMATCH");
    }

    [Fact]
    public async Task CreateAsync_rejects_when_session_not_open()
    {
        var (svc, db, clock, session) = BuildContextAsync();
        session.Status = CashSessionStatus.CLOSED;
        await db.SaveChangesAsync();

        var dto = new CreateCashOperationDto(
            session.Id, clock.UtcNow, OperationDirection.IN,
            100, 100, PaymentMethod.CASH, "x", null, null, null);

        await FluentActions.Awaiting(() => svc.CreateAsync(dto))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "CASH_SESSION_NOT_OPEN");
    }

    [Fact]
    public async Task CancelAsync_soft_deletes_and_records_reason()
    {
        var (svc, db, clock, session) = BuildContextAsync();
        var created = await svc.CreateAsync(new CreateCashOperationDto(
            session.Id, clock.UtcNow, OperationDirection.IN, 100, 100m,
            PaymentMethod.CASH, "v", null, null, null));

        await svc.CancelAsync(created.Id, new CancelCashOperationDto("erreur de saisie"));

        var op = db.CashOperations.IgnoreQueryFilters().Single();
        op.IsDeleted.Should().BeTrue();
        op.DeleteReason.Should().Be("erreur de saisie");
        op.DeletedBy.Should().Be(1);
    }

    [Fact]
    public async Task CancelAsync_pending_approval_bypasses_cancel_workflow_even_when_feature_and_rule_match()
    {
        var (db, clock) = TestDbContextFactory.Create();
        TestDbContextFactory.SeedMinimalAsync(db).GetAwaiter().GetResult();
        var session = new CashSession
        {
            CashRegisterId = 10, OpenedBy = 1, OpenedAt = clock.UtcNow,
            OpeningBalance = 0, Status = CashSessionStatus.OPEN
        };
        db.CashSessions.Add(session);
        await db.SaveChangesAsync();

        var user = new FakeCurrentUser();
        var refGen = Substitute.For<IReferenceGeneratorService>();
        refGen.NextOperationRefAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"OP-{DateTime.UtcNow.Ticks}");
        var sessions = Substitute.For<ICashSessionService>();
        var settings = Substitute.For<ISettingsService>();
        var approval = Substitute.For<CashApp.Application.Approvals.IApprovalService>();
        var features = Substitute.For<CashApp.Application.Common.Interfaces.IFeatureService>();
        // AdvValidation ACTIVÉ pour ce test.
        features.IsEnabledAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        // Et une règle CASH_OPERATION_CANCEL existe et matche.
        approval.FindMatchingRuleAsync(
            CashApp.Domain.Enums.ApprovalTargetType.CASH_OPERATION_CANCEL,
            Arg.Any<decimal?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CashApp.Domain.Entities.V2.ApprovalRequest { ApprovalRuleId = 42 });
        var audit = Substitute.For<CashApp.Application.Common.Interfaces.IAuditLogger>();
        var access = Substitute.For<CashApp.Application.Common.Interfaces.ICashRegisterAccessService>();
        access.GetAccessibleRegisterIdsAsync(Arg.Any<CancellationToken>()).Returns(new[] { 10 });
        access.CanAccessAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        var thirdParties = Substitute.For<CashApp.Application.ThirdParties.IThirdPartyService>();
        var svc = new CashOperationService(db, user, clock, refGen, sessions, settings, approval, features, audit, access, thirdParties);

        var op = new CashOperation
        {
            OperationRef = "OP-1", CashRegisterId = 10, CashSessionId = session.Id,
            OperationDate = clock.UtcNow, Direction = OperationDirection.IN,
            CategoryId = 100, Amount = 500, CurrencyCode = "XOF",
            PaymentMethod = PaymentMethod.CASH, Label = "à valider",
            CreatedBy = 1, IsPendingApproval = true
        };
        db.CashOperations.Add(op);
        await db.SaveChangesAsync();

        await svc.CancelAsync(op.Id, new CancelCashOperationDto("annulation directe"));

        var after = db.CashOperations.IgnoreQueryFilters().Single(o => o.Id == op.Id);
        after.IsDeleted.Should().BeTrue();
        after.IsPendingApproval.Should().BeFalse();

        // CRITIQUE : aucune CreateRequestAsync(CASH_OPERATION_CANCEL,...) ne doit avoir été appelée.
        await approval.DidNotReceive().CreateRequestAsync(
            Arg.Any<int>(),
            CashApp.Domain.Enums.ApprovalTargetType.CASH_OPERATION_CANCEL,
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int?>(),
            Arg.Any<decimal?>(), Arg.Any<string?>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_pending_approval_op_is_directly_deleted_and_closes_pending_request()
    {
        var (svc, db, clock, session) = BuildContextAsync();
        var created = await svc.CreateAsync(new CreateCashOperationDto(
            session.Id, clock.UtcNow, OperationDirection.IN, 100, 500m,
            PaymentMethod.CASH, "à valider", null, null, null));

        // Simule une opération encore en attente d'approbation (création non validée)
        // avec sa demande d'approbation PENDING associée.
        var op = db.CashOperations.IgnoreQueryFilters().Single(o => o.Id == created.Id);
        op.IsPendingApproval = true;
        var rule = new CashApp.Domain.Entities.V2.ApprovalRule
        {
            Code = "OP_CREATE", Name = "Op create",
            TargetType = ApprovalTargetType.CASH_OPERATION,
            RequiredApproverRole = "ADMIN", IsActive = true
        };
        db.ApprovalRules.Add(rule);
        await db.SaveChangesAsync();
        var pendingReq = new CashApp.Domain.Entities.V2.ApprovalRequest
        {
            RequestRef = "AR-TEST-1",
            ApprovalRuleId = rule.Id,
            TargetType = ApprovalTargetType.CASH_OPERATION,
            TargetEntityType = nameof(CashOperation),
            TargetEntityId = op.Id,
            Reason = "création",
            Status = ApprovalStatus.PENDING,
            RequestedBy = 1,
            RequestedAt = clock.UtcNow
        };
        db.ApprovalRequests.Add(pendingReq);
        await db.SaveChangesAsync();

        await svc.CancelAsync(created.Id, new CancelCashOperationDto("annulation directe avant validation"));

        var deleted = db.CashOperations.IgnoreQueryFilters().Single(o => o.Id == created.Id);
        deleted.IsDeleted.Should().BeTrue();
        deleted.IsPendingApproval.Should().BeFalse();
        deleted.IsPendingCancellation.Should().BeFalse();
        deleted.DeleteReason.Should().Be("annulation directe avant validation");

        // La demande d'approbation associée doit être SUPPRIMÉE de la liste, pas juste marquée annulée.
        db.ApprovalRequests.Any(r => r.Id == pendingReq.Id).Should().BeFalse();
        db.ApprovalRequests
            .Count(r => r.TargetEntityId == op.Id
                        && (r.TargetType == ApprovalTargetType.CASH_OPERATION
                            || r.TargetType == ApprovalTargetType.CASH_OPERATION_CANCEL))
            .Should().Be(0);
    }
}
