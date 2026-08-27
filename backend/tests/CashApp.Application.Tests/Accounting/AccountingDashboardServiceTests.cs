using CashApp.Application.Accounting;
using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Tests.Fakes;
using CashApp.Application.Tests.Infrastructure;
using CashApp.Domain.Entities;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using CashApp.Infrastructure.Persistence;
using CashApp.Infrastructure.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CashApp.Application.Tests.Accounting;

public class AccountingDashboardServiceTests
{
    private sealed record Context(
        AccountingGenerationEngineService Engine,
        AccountingDashboardService Dashboard,
        AccountingExportService Export,
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
        var batchReader = new AccountingGenerationService(db, clock, audit);
        var engine = new AccountingGenerationEngineService(db, clock, user, audit, batchReader);
        var entryService = new AccountingEntryService(db, clock, audit);
        var dashboard = new AccountingDashboardService(db, clock);
        var export = AccountingExportServiceTestFactory.Create(db, clock, entryService, user, audit);

        return new Context(engine, dashboard, export, db, clock, session);
    }

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
    public async Task GetAsync_reports_counts_after_generation_and_export()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-DASH-1");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        await ctx.Export.ExportGenerationAsync(batch.Id);

        var dash = await ctx.Dashboard.GetAsync();

        dash.AccountCount.Should().Be(2);
        dash.JournalCount.Should().Be(1);
        dash.ConfiguredCategoryCount.Should().Be(1);
        dash.ConfiguredCashRegisterCount.Should().Be(1);
        dash.BatchCount.Should().Be(1);
        dash.EntryCount.Should().Be(2);
        dash.BatchesToday.Should().Be(1);
        dash.EntriesToday.Should().Be(2);
        dash.ExportsToday.Should().Be(1);
        dash.LastGenerationReference.Should().Be(batch.Reference);
        dash.LastExportFileName.Should().NotBeNullOrEmpty();
        dash.EntriesByDay.Sum(d => d.Count).Should().Be(2);
        dash.JournalDistribution.Should().ContainSingle(d => d.Name == "VE" && d.Count == 2);
    }

    [Fact]
    public async Task GetAsync_counts_failed_queue_items_as_errors()
    {
        var ctx = BuildContext();
        ctx.Db.AccountingGenerationQueues.Add(new AccountingGenerationQueue
        {
            CreatedDate = ctx.Clock.UtcNow, RequestedBy = 1, GenerationMode = AccountingGenerationMode.MANUAL,
            StartDate = ctx.Clock.UtcNow, EndDate = ctx.Clock.UtcNow, Status = QueueStatus.FAILED
        });
        ctx.Db.SaveChanges();

        var dash = await ctx.Dashboard.GetAsync();
        dash.ErrorsCount.Should().Be(1);
    }

    [Fact]
    public async Task ExportGenerationAsync_persists_downloadable_log()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 500m, ctx.Clock.UtcNow, "OP-EXPLOG-1");
        var batch = await ctx.Engine.GenerateAsync(new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));

        var result = await ctx.Export.ExportGenerationAsync(batch.Id);
        var logs = await ctx.Export.ListLogsAsync(new AccountingExportLogFilterDto());
        logs.TotalCount.Should().Be(1);
        logs.Items[0].FileName.Should().Be(result.FileName);
        logs.Items[0].GenerationReference.Should().Be(batch.Reference);

        var downloaded = await ctx.Export.DownloadLogAsync(logs.Items[0].Id);
        downloaded.Content.Should().BeEquivalentTo(result.Content);
        downloaded.FileName.Should().Be(result.FileName);
    }
}
