using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Tests.Fakes;
using CashApp.Application.Tests.Infrastructure;
using CashApp.Domain.Entities;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using CashApp.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CashApp.Application.Tests.Accounting;

public class AccountingGenerationEngineServiceTests
{
    private static (AccountingGenerationEngineService Engine, AppDbContext Db, FakeClock Clock, CashSession Session)
        BuildContext(AccountingGenerationType generationType = AccountingGenerationType.DETAILED, bool isConfigured = true)
    {
        var (db, clock) = TestDbContextFactory.Create();
        TestDbContextFactory.SeedMinimalAsync(db).GetAwaiter().GetResult();

        var journal = new AccountingJournal { Code = "VE", Name = "Ventes", IsActive = true };
        var cashAccount = new AccountingAccount { AccountNumber = "571000", Name = "Caisse", Nature = AccountingAccountNature.CASH, IsActive = true };
        var saleAccount = new AccountingAccount { AccountNumber = "701000", Name = "Ventes", Nature = AccountingAccountNature.REVENUE, IsActive = true };
        var purchaseAccount = new AccountingAccount { AccountNumber = "601000", Name = "Achats", Nature = AccountingAccountNature.EXPENSE, IsActive = true };
        db.AccountingJournals.Add(journal);
        db.AccountingAccounts.AddRange(cashAccount, saleAccount, purchaseAccount);
        db.SaveChanges();

        var register = db.CashRegisters.Single(r => r.Id == 10);
        register.AccountingJournalId = journal.Id;
        register.AccountingAccountId = cashAccount.Id;
        db.Categories.Single(c => c.Id == 100).AccountingAccountId = saleAccount.Id; // SALE / IN
        db.Categories.Single(c => c.Id == 101).AccountingAccountId = purchaseAccount.Id; // PURCHASE / OUT

        db.AccountingSettings.Add(new AccountingSettings
        {
            GenerationType = generationType,
            GenerationMode = AccountingGenerationMode.MANUAL,
            NarrationTemplate = "{Category} - {CashRegister} - {OperationNumber}",
            IsConfigured = isConfigured
        });

        var session = new CashSession
        {
            CashRegisterId = 10, OpenedBy = 1, OpenedAt = clock.UtcNow,
            OpeningBalance = 0, Status = CashSessionStatus.CLOSED
        };
        db.CashSessions.Add(session);
        db.SaveChanges();

        var user = new FakeCurrentUser();
        var audit = Substitute.For<IAuditLogger>();
        var reader = new AccountingGenerationService(db, clock, audit);
        var engine = new AccountingGenerationEngineService(db, clock, user, audit, reader);

        return (engine, db, clock, session);
    }

    private static CashOperation MakeOperation(AppDbContext db, CashSession session, OperationDirection direction, int categoryId, decimal amount, DateTime date, string reference)
    {
        var op = new CashOperation
        {
            OperationRef = reference,
            CashRegisterId = session.CashRegisterId,
            CashSessionId = session.Id,
            OperationDate = date,
            Direction = direction,
            CategoryId = categoryId,
            Amount = amount,
            CurrencyCode = "XOF",
            PaymentMethod = PaymentMethod.CASH,
            Label = "op",
            CreatedBy = 1
        };
        db.CashOperations.Add(op);
        db.SaveChanges();
        return op;
    }

    [Fact]
    public async Task GenerateAsync_creates_debit_cash_credit_category_for_IN_operation()
    {
        var (engine, db, clock, session) = BuildContext();
        MakeOperation(db, session, OperationDirection.IN, 100, 1000m, clock.UtcNow, "OP-IN-1");

        var result = await engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));

        result.Entries.Should().HaveCount(2);
        var debit = result.Entries.Single(e => e.Debit == 1000m);
        var credit = result.Entries.Single(e => e.Credit == 1000m);
        debit.AccountNumber.Should().Be("571000"); // caisse débitée
        credit.AccountNumber.Should().Be("701000"); // catégorie créditée
        debit.Description.Should().Be("Vente - C1 - OP-IN-1");
    }

    [Fact]
    public async Task GenerateAsync_excludes_operations_whose_session_is_still_open()
    {
        var (engine, db, clock, closedSession) = BuildContext();
        var openSession = new CashSession
        {
            CashRegisterId = 10, OpenedBy = 1, OpenedAt = clock.UtcNow,
            OpeningBalance = 0, Status = CashSessionStatus.OPEN
        };
        db.CashSessions.Add(openSession);
        db.SaveChanges();

        MakeOperation(db, closedSession, OperationDirection.IN, 100, 1000m, clock.UtcNow, "OP-CLOSED-1");
        MakeOperation(db, openSession, OperationDirection.IN, 100, 500m, clock.UtcNow, "OP-OPEN-1");

        var first = await engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));

        first.TotalOperations.Should().Be(1);
        first.Entries.Should().OnlyContain(e => e.CashOperationRef == "OP-CLOSED-1");

        // Une fois sa session clôturée, l'opération devient éligible lors d'une génération suivante.
        openSession.Status = CashSessionStatus.CLOSED;
        db.SaveChanges();

        var second = await engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));

        second.Entries.Should().Contain(e => e.CashOperationRef == "OP-OPEN-1");
    }

    [Fact]
    public async Task GenerateAsync_creates_debit_category_credit_cash_for_OUT_operation()
    {
        var (engine, db, clock, session) = BuildContext();
        MakeOperation(db, session, OperationDirection.OUT, 101, 400m, clock.UtcNow, "OP-OUT-1");

        var result = await engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));

        result.Entries.Should().HaveCount(2);
        var debit = result.Entries.Single(e => e.Debit == 400m);
        var credit = result.Entries.Single(e => e.Credit == 400m);
        debit.AccountNumber.Should().Be("601000"); // catégorie débitée
        credit.AccountNumber.Should().Be("571000"); // caisse créditée
    }

    [Fact]
    public async Task GenerateAsync_never_processes_same_operation_twice()
    {
        var (engine, db, clock, session) = BuildContext();
        MakeOperation(db, session, OperationDirection.IN, 100, 1000m, clock.UtcNow, "OP-DUP-1");

        var first = await engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));
        first.Entries.Should().HaveCount(2);

        // Deuxième exécution sur la même période : aucune nouvelle écriture pour l'opération déjà comptabilisée.
        var entriesBefore = db.AccountingEntries.Count();
        var second = await engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));
        second.Entries.Should().BeEmpty();
        db.AccountingEntries.Count().Should().Be(entriesBefore);
    }

    [Fact]
    public async Task GenerateAsync_creates_pending_when_category_account_missing()
    {
        var (engine, db, clock, session) = BuildContext();
        db.Categories.Single(c => c.Id == 100).AccountingAccountId = null; // casse volontairement le paramétrage
        db.SaveChanges();
        MakeOperation(db, session, OperationDirection.IN, 100, 1000m, clock.UtcNow, "OP-PENDING-1");

        var result = await engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));

        result.Entries.Should().BeEmpty();
        db.AccountingPendings.Should().HaveCount(1);
        db.AccountingPendings.Single().Reason.Should().Contain("Compte catégorie absent");
    }

    [Fact]
    public async Task GeneratePendingAsync_resolves_once_configuration_is_fixed()
    {
        var (engine, db, clock, session) = BuildContext();
        var brokenCategory = db.Categories.Single(c => c.Id == 100);
        var originalAccountId = brokenCategory.AccountingAccountId;
        brokenCategory.AccountingAccountId = null;
        db.SaveChanges();
        MakeOperation(db, session, OperationDirection.IN, 100, 750m, clock.UtcNow, "OP-RELANCE-1");

        await engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));
        db.AccountingPendings.Single().Resolved.Should().BeFalse();

        // Le paramétrage est corrigé.
        brokenCategory.AccountingAccountId = originalAccountId;
        db.SaveChanges();

        var retried = await engine.GeneratePendingAsync();

        retried.Entries.Should().HaveCount(2);
        db.AccountingPendings.Single().Resolved.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAsync_centralizes_cash_side_entries_by_journal_and_account()
    {
        var (engine, db, clock, session) = BuildContext(AccountingGenerationType.CENTRALIZED);
        MakeOperation(db, session, OperationDirection.IN, 100, 1000m, clock.UtcNow, "OP-C-1");
        MakeOperation(db, session, OperationDirection.IN, 100, 500m, clock.UtcNow, "OP-C-2");

        var result = await engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));

        // 2 lignes catégorie individuelles + 1 ligne caisse groupée = 3.
        result.Entries.Should().HaveCount(3);
        var grouped = result.Entries.Single(e => e.CashOperationId == null);
        grouped.Debit.Should().Be(1500m);
        grouped.AccountNumber.Should().Be("571000");
        result.Entries.Count(e => e.CashOperationId != null).Should().Be(2);
    }

    [Fact]
    public async Task PreviewAsync_reports_counts_without_persisting_anything()
    {
        var (engine, db, clock, session) = BuildContext();
        MakeOperation(db, session, OperationDirection.IN, 100, 1000m, clock.UtcNow, "OP-PREVIEW-1");

        var preview = await engine.PreviewAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null));

        preview.OperationCount.Should().Be(1);
        preview.EstimatedEntryCount.Should().Be(2);
        preview.PendingCount.Should().Be(0);
        db.AccountingEntries.Should().BeEmpty();
        db.AccountingGenerations.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_throws_when_engine_not_configured()
    {
        var (engine, db, clock, session) = BuildContext(isConfigured: false);
        MakeOperation(db, session, OperationDirection.IN, 100, 1000m, clock.UtcNow, "OP-NOCFG-1");

        await FluentActions.Awaiting(() => engine.GenerateAsync(new GenerateAccountingEntriesDto(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1), null)))
            .Should().ThrowAsync<ValidationException>();
    }
}
