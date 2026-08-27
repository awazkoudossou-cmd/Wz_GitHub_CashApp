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
using CashApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CashApp.Application.Tests.Accounting;

public class AccountingLedgerServiceTests
{
    private sealed record Context(
        AccountingGenerationEngineService Engine,
        AccountingGenerationService BatchService,
        AccountingEntryService EntryService,
        AppDbContext Db,
        FakeClock Clock,
        CashSession Session);

    private static Context BuildContext()
    {
        var (db, clock) = TestDbContextFactory.Create();
        TestDbContextFactory.SeedMinimalAsync(db).GetAwaiter().GetResult();

        var journal = new AccountingJournal { Code = "VE", Name = "Ventes", IsActive = true };
        var cashAccount = new AccountingAccount { AccountNumber = "571000", Name = "Caisse", Nature = AccountingAccountNature.CASH, IsActive = true };
        var saleAccount = new AccountingAccount { AccountNumber = "701000", Name = "Ventes", Nature = AccountingAccountNature.REVENUE, IsActive = true };
        db.AccountingJournals.Add(journal);
        db.AccountingAccounts.AddRange(cashAccount, saleAccount);
        db.SaveChanges();

        var register = db.CashRegisters.Single(r => r.Id == 10);
        register.AccountingJournalId = journal.Id;
        register.AccountingAccountId = cashAccount.Id;
        db.Categories.Single(c => c.Id == 100).AccountingAccountId = saleAccount.Id; // SALE / IN

        db.AccountingSettings.Add(new AccountingSettings
        {
            GenerationType = AccountingGenerationType.DETAILED,
            GenerationMode = AccountingGenerationMode.MANUAL,
            NarrationTemplate = "{Category} - {CashRegister} - {OperationNumber}",
            IsConfigured = true
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
        var batchService = new AccountingGenerationService(db, clock, audit);
        var engine = new AccountingGenerationEngineService(db, clock, user, audit, batchService);
        var entryService = new AccountingEntryService(db, clock, audit);

        return new Context(engine, batchService, entryService, db, clock, session);
    }

    private static AccountingEntryFilterDto EmptyFilter() => new(
        From: null, To: null, JournalId: null, AccountId: null, CashRegisterId: null, CategoryId: null,
        UserId: null, GenerationId: null, Locked: null, Reference: null, PieceNumber: null, Search: null, SortBy: null);

    private static CashOperation MakeOperation(AppDbContext db, CashSession session, OperationDirection direction, int categoryId, decimal amount, DateTime date, string reference)
    {
        var op = new CashOperation
        {
            OperationRef = reference, CashRegisterId = session.CashRegisterId, CashSessionId = session.Id,
            OperationDate = date, Direction = direction, CategoryId = categoryId, Amount = amount,
            CurrencyCode = "XOF", PaymentMethod = PaymentMethod.CASH, Label = "op", CreatedBy = 1
        };
        db.CashOperations.Add(op);
        db.SaveChanges();
        return op;
    }

    [Fact]
    public async Task DeleteAsync_removes_entries_and_logs_but_not_cash_operations()
    {
        var ctx = BuildContext();
        var op = MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-DEL-1");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        batch.Entries.Should().HaveCount(2);

        await ctx.BatchService.DeleteAsync(batch.Id);

        ctx.Db.AccountingEntries.Should().BeEmpty();
        ctx.Db.AccountingGenerationLogs.Should().BeEmpty();
        ctx.Db.AccountingGenerations.Any(g => g.Id == batch.Id).Should().BeFalse();
        ctx.Db.CashOperations.Any(o => o.Id == op.Id).Should().BeTrue(); // jamais supprimée
    }

    [Fact]
    public async Task DeleteAsync_blocked_when_batch_exported()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-DEL-2");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));

        var entity = ctx.Db.AccountingGenerations.Single(g => g.Id == batch.Id);
        entity.Exported = true;
        ctx.Db.SaveChanges();

        await FluentActions.Awaiting(() => ctx.BatchService.DeleteAsync(batch.Id))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "BATCH_EXPORTED");
    }

    [Fact]
    public async Task UpdateAsync_blocked_when_batch_exported_readonly()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-LOCK-1");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));

        var entity = ctx.Db.AccountingGenerations.Single(g => g.Id == batch.Id);
        entity.Exported = true;
        ctx.Db.SaveChanges();

        var entryId = batch.Entries.First().Id;
        await FluentActions.Awaiting(() => ctx.EntryService.UpdateAsync(entryId, new UpdateAccountingEntryDto("nouveau libellé", null)))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "ENTRY_LOCKED");
    }

    [Fact]
    public async Task UpdateAsync_allows_description_and_comment_when_not_locked()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-EDIT-1");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        var entryId = batch.Entries.First().Id;

        var updated = await ctx.EntryService.UpdateAsync(entryId, new UpdateAccountingEntryDto("Libellé corrigé", "Vérifié par le superviseur"));

        updated.Description.Should().Be("Libellé corrigé");
        updated.Comment.Should().Be("Vérifié par le superviseur");
        updated.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task RegenerateAsync_replaces_entries_on_the_same_batch()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-REGEN-1");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        var originalEntryIds = batch.Entries.Select(e => e.Id).ToList();

        var regenerated = await ctx.Engine.RegenerateAsync(batch.Id);

        regenerated.Id.Should().Be(batch.Id);
        regenerated.Reference.Should().Be(batch.Reference); // même batch, pas un nouveau
        regenerated.Entries.Should().HaveCount(2);
        regenerated.Entries.Select(e => e.Id).Should().NotIntersectWith(originalEntryIds); // anciennes lignes bien remplacées
    }

    [Fact]
    public async Task RegenerateAsync_blocked_when_batch_exported()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-REGEN-2");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        var entity = ctx.Db.AccountingGenerations.Single(g => g.Id == batch.Id);
        entity.Exported = true;
        ctx.Db.SaveChanges();

        await FluentActions.Awaiting(() => ctx.Engine.RegenerateAsync(batch.Id))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "BATCH_EXPORTED");
    }

    [Fact]
    public async Task ListAsync_filters_by_journal_account_and_full_text_search()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-FILTER-1");
        await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));

        var byJournal = await ctx.EntryService.ListAsync(EmptyFilter() with { JournalId = 1 });
        byJournal.TotalCount.Should().Be(2);

        var cashAccountId = ctx.Db.AccountingAccounts.Single(a => a.AccountNumber == "571000").Id;
        var byAccount = await ctx.EntryService.ListAsync(EmptyFilter() with { AccountId = cashAccountId });
        byAccount.Items.Should().OnlyContain(e => e.AccountNumber == "571000");

        var bySearch = await ctx.EntryService.ListAsync(EmptyFilter() with { Search = "OP-FILTER-1" });
        bySearch.TotalCount.Should().Be(2);

        var noMatch = await ctx.EntryService.ListAsync(EmptyFilter() with { Search = "INEXISTANT" });
        noMatch.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ListAsync_sorts_by_debit_ascending_and_descending()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 500m, ctx.Clock.UtcNow, "OP-SORT-1");
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 2000m, ctx.Clock.UtcNow, "OP-SORT-2");
        await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));

        var asc = await ctx.EntryService.ListAsync(EmptyFilter() with { SortBy = "debit", SortDescending = false });
        var debitValuesAsc = asc.Items.Select(e => e.Debit).ToList();
        debitValuesAsc.Should().BeInAscendingOrder();

        var desc = await ctx.EntryService.ListAsync(EmptyFilter() with { SortBy = "debit", SortDescending = true });
        var debitValuesDesc = desc.Items.Select(e => e.Debit).ToList();
        debitValuesDesc.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ListAsync_paginates_server_side()
    {
        var ctx = BuildContext();
        for (var i = 1; i <= 5; i++)
            MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 100m * i, ctx.Clock.UtcNow, $"OP-PAGE-{i}");
        await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        // 5 opérations IN détaillées => 10 écritures au total.

        var page1 = await ctx.EntryService.ListAsync(EmptyFilter() with { Page = 1, PageSize = 4 });
        page1.Items.Should().HaveCount(4);
        page1.TotalCount.Should().Be(10);
        page1.TotalPages.Should().Be(3);

        var page3 = await ctx.EntryService.ListAsync(EmptyFilter() with { Page = 3, PageSize = 4 });
        page3.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExportEntriesAsync_produces_a_non_empty_workbook()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-EXPORT-1");
        await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));

        var exportService = AccountingExportServiceTestFactory.Create(ctx.Db, ctx.Clock, ctx.EntryService);

        var result = await exportService.ExportEntriesAsync(EmptyFilter());

        result.Content.Should().NotBeEmpty();
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.FileName.Should().StartWith("ACCOUNTING_");
    }
}
