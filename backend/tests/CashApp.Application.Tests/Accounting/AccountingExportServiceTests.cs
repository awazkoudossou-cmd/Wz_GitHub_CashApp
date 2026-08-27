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

public class AccountingExportServiceTests
{
    private sealed record Context(
        AccountingGenerationEngineService Engine,
        AccountingEntryService EntryService,
        AccountingExportService ExportService,
        AppDbContext Db,
        FakeClock Clock,
        CashSession Session,
        AccountingJournal Journal,
        AccountingAccount CashAccount,
        AccountingAccount SaleAccount);

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
        db.Categories.Single(c => c.Id == 100).AccountingAccountId = saleAccount.Id;

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
        var exportService = AccountingExportServiceTestFactory.Create(db, clock, entryService, user, audit);

        return new Context(engine, entryService, exportService, db, clock, session, journal, cashAccount, saleAccount);
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
    public async Task PreviewExportAsync_computes_counts_period_and_balance()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1500m, ctx.Clock.UtcNow, "OP-PREV-1");
        await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));

        var preview = await ctx.ExportService.PreviewExportAsync(EmptyFilter());

        preview.EntryCount.Should().Be(2);
        preview.BatchCount.Should().Be(1);
        preview.IsBalanced.Should().BeTrue();
        preview.TotalDebit.Should().Be(preview.TotalCredit);
    }

    [Fact]
    public async Task ExportEntriesAsync_writes_file_to_disk_and_creates_history_row()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-EXP-1");
        await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));

        var result = await ctx.ExportService.ExportEntriesAsync(EmptyFilter());

        var log = ctx.Db.AccountingExportLogs.Single();
        log.ExportNumber.Should().StartWith("EXP-");
        log.Status.Should().Be(AccountingExportStatus.GENERATED);
        log.FilePath.Should().NotBeNullOrEmpty();
        File.Exists(log.FilePath).Should().BeTrue();
        result.FileName.Should().StartWith("ACCOUNTING_");
    }

    [Fact]
    public async Task ExportGenerationAsync_locks_the_batch_and_names_file_with_reference()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-EXP-2");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));

        var result = await ctx.ExportService.ExportGenerationAsync(batch.Id);

        result.FileName.Should().Be($"ACCOUNTING_BATCH_{batch.Reference}.xlsx");
        ctx.Db.AccountingGenerations.Single(g => g.Id == batch.Id).Exported.Should().BeTrue();
        ctx.Db.AccountingExportLogs.Single().ExportType.Should().Be(AccountingExportType.Batch);
    }

    [Fact]
    public async Task ExportEntriesAsync_throws_when_debit_and_credit_are_unbalanced()
    {
        var ctx = BuildContext();
        var generation = new AccountingGeneration
        {
            Reference = "ACC-TEST-UNBALANCED", GenerationType = AccountingGenerationType.DETAILED,
            GenerationMode = AccountingGenerationMode.MANUAL, StartDate = ctx.Clock.UtcNow.AddDays(-1), EndDate = ctx.Clock.UtcNow,
            Status = AccountingGenerationStatus.GENERATED, GeneratedBy = 1, GeneratedAt = ctx.Clock.UtcNow
        };
        ctx.Db.AccountingGenerations.Add(generation);
        ctx.Db.SaveChanges();

        ctx.Db.AccountingEntries.Add(new AccountingEntry
        {
            GenerationId = generation.Id, JournalId = ctx.Journal.Id, AccountId = ctx.CashAccount.Id,
            EntryDate = ctx.Clock.UtcNow, OperationDate = ctx.Clock.UtcNow, Reference = "REF-1",
            Description = "Déséquilibrée", Debit = 1000m, Credit = 0m
        });
        ctx.Db.SaveChanges();

        await FluentActions.Awaiting(() => ctx.ExportService.ExportEntriesAsync(EmptyFilter() with { GenerationId = generation.Id }))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "EXPORT_UNBALANCED");

        ctx.Db.AccountingExportLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadLogAsync_transitions_status_from_generated_to_downloaded_on_first_download_only()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-DL-1");
        await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        await ctx.ExportService.ExportEntriesAsync(EmptyFilter());
        var logId = ctx.Db.AccountingExportLogs.Single().Id;

        var download1 = await ctx.ExportService.DownloadLogAsync(logId);
        download1.Content.Should().NotBeEmpty();

        var afterFirst = ctx.Db.AccountingExportLogs.Single(l => l.Id == logId);
        afterFirst.Status.Should().Be(AccountingExportStatus.DOWNLOADED);
        afterFirst.DownloadedAt.Should().NotBeNull();
        var firstDownloadedAt = afterFirst.DownloadedAt;

        ctx.Clock.UtcNow = ctx.Clock.UtcNow.AddMinutes(5);
        await ctx.ExportService.DownloadLogAsync(logId);
        ctx.Db.AccountingExportLogs.Single(l => l.Id == logId).DownloadedAt.Should().Be(firstDownloadedAt); // pas re-timestampé
    }

    [Fact]
    public async Task ReexportAsync_creates_a_new_history_row_without_touching_entries()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-REEXP-1");
        await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        await ctx.ExportService.ExportEntriesAsync(EmptyFilter());
        var originalLogId = ctx.Db.AccountingExportLogs.Single().Id;
        var entryCountBefore = ctx.Db.AccountingEntries.Count();

        var result = await ctx.ExportService.ReexportAsync(originalLogId);

        result.Content.Should().NotBeEmpty();
        ctx.Db.AccountingExportLogs.Should().HaveCount(2);
        ctx.Db.AccountingEntries.Count().Should().Be(entryCountBefore);
    }

    [Fact]
    public async Task DeleteExportAsync_marks_deleted_and_removes_file_but_keeps_batch_and_entries()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-DEL-EXP-1");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        await ctx.ExportService.ExportEntriesAsync(EmptyFilter());
        var log = ctx.Db.AccountingExportLogs.Single();
        var filePath = log.FilePath;
        File.Exists(filePath).Should().BeTrue();

        await ctx.ExportService.DeleteExportAsync(log.Id);

        ctx.Db.AccountingExportLogs.Single(l => l.Id == log.Id).Status.Should().Be(AccountingExportStatus.DELETED);
        File.Exists(filePath).Should().BeFalse();
        ctx.Db.AccountingGenerations.Any(g => g.Id == batch.Id).Should().BeTrue();
        ctx.Db.AccountingEntries.Any().Should().BeTrue();
    }

    [Fact]
    public async Task ListLogsAsync_filters_by_status()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-HIST-1");
        await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        await ctx.ExportService.ExportEntriesAsync(EmptyFilter());
        var logId = ctx.Db.AccountingExportLogs.Single().Id;
        await ctx.ExportService.DeleteExportAsync(logId);

        var deleted = await ctx.ExportService.ListLogsAsync(new AccountingExportLogFilterDto(Status: AccountingExportStatus.DELETED));
        deleted.Items.Should().ContainSingle(l => l.Id == logId);

        var generated = await ctx.ExportService.ListLogsAsync(new AccountingExportLogFilterDto(Status: AccountingExportStatus.GENERATED));
        generated.Items.Should().BeEmpty();
    }
}
