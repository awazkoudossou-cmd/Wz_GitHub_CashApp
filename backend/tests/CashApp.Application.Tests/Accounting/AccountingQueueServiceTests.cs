using System.Diagnostics;
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

public class AccountingQueueServiceTests
{
    private sealed record Context(
        AccountingQueueService Queue,
        AccountingRetryService Retry,
        AccountingGenerationEngineService Engine,
        AppDbContext Db,
        FakeClock Clock,
        CashSession Session);

    private static Context BuildContext(bool isConfigured = true)
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
        var batchReader = new AccountingGenerationService(db, clock, audit);
        var engine = new AccountingGenerationEngineService(db, clock, user, audit, batchReader);
        var queue = new AccountingQueueService(db, clock, user, audit);
        var retry = new AccountingRetryService(db, clock, audit, queue);

        return new Context(queue, retry, engine, db, clock, session);
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
    public async Task EnqueueManualAsync_creates_pending_item()
    {
        var ctx = BuildContext();
        var item = await ctx.Queue.EnqueueManualAsync(new EnqueueManualGenerationDto(
            ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), new[] { 10 }));

        item.Status.Should().Be(QueueStatus.PENDING);
        item.GenerationMode.Should().Be(AccountingGenerationMode.MANUAL);
        item.RequestedBy.Should().Be(1);
        item.CashRegisterIds.Should().BeEquivalentTo(new[] { 10 });
    }

    [Fact]
    public async Task EnqueueManualAsync_rejects_when_not_configured()
    {
        var ctx = BuildContext(isConfigured: false);
        await FluentActions.Awaiting(() => ctx.Queue.EnqueueManualAsync(
                new EnqueueManualGenerationDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow, null)))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "ACCOUNTING_NOT_CONFIGURED");
    }

    [Fact]
    public async Task CancelAsync_cancels_pending_but_not_processing()
    {
        var ctx = BuildContext();
        var pending = await ctx.Queue.EnqueueManualAsync(new EnqueueManualGenerationDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow, null));
        var cancelled = await ctx.Queue.CancelAsync(pending.Id);
        cancelled.Status.Should().Be(QueueStatus.CANCELLED);

        var processing = await ctx.Queue.EnqueueManualAsync(new EnqueueManualGenerationDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow, null));
        var entity = ctx.Db.AccountingGenerationQueues.Single(q => q.Id == processing.Id);
        entity.Status = QueueStatus.PROCESSING;
        ctx.Db.SaveChanges();

        await FluentActions.Awaiting(() => ctx.Queue.CancelAsync(processing.Id))
            .Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "QUEUE_NOT_CANCELLABLE");
    }

    [Fact]
    public async Task RetryAsync_resets_to_pending_and_increments_count_until_max()
    {
        var ctx = BuildContext();
        var item = await ctx.Queue.EnqueueManualAsync(new EnqueueManualGenerationDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow, null));
        var entity = ctx.Db.AccountingGenerationQueues.Single(q => q.Id == item.Id);

        // Non FAILED : retry refusé.
        await FluentActions.Awaiting(() => ctx.Retry.RetryAsync(item.Id))
            .Should().ThrowAsync<BusinessRuleException>().Where(e => e.Code == "QUEUE_NOT_FAILED");

        entity.Status = QueueStatus.FAILED;
        entity.RetryCount = AccountingRetryService.MaxRetries;
        ctx.Db.SaveChanges();

        await FluentActions.Awaiting(() => ctx.Retry.RetryAsync(item.Id))
            .Should().ThrowAsync<BusinessRuleException>().Where(e => e.Code == "QUEUE_MAX_RETRIES");

        entity.RetryCount = 0;
        ctx.Db.SaveChanges();
        var retried = await ctx.Retry.RetryAsync(item.Id);
        retried.Status.Should().Be(QueueStatus.PENDING);
        retried.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task PreviewAsync_reports_ignored_and_already_accounted_counts()
    {
        var ctx = BuildContext();
        var ok = MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-PREV-OK");
        var cancelled = MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 500m, ctx.Clock.UtcNow, "OP-PREV-CANCELLED");
        cancelled.IsDeleted = true;
        var pendingApproval = MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 300m, ctx.Clock.UtcNow, "OP-PREV-PENDING-APPROVAL");
        pendingApproval.IsPendingApproval = true;
        ctx.Db.SaveChanges();

        var range = new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null);
        var preview = await ctx.Engine.PreviewAsync(range);

        preview.OperationCount.Should().Be(1); // seule "ok" est POSTED et non comptabilisée
        preview.IgnoredCount.Should().Be(2); // annulée + en attente de validation
        preview.AlreadyAccountedCount.Should().Be(0);

        await ctx.Engine.GenerateAsync(range);
        var previewAfter = await ctx.Engine.PreviewAsync(range);
        previewAfter.OperationCount.Should().Be(0);
        previewAfter.AlreadyAccountedCount.Should().Be(1); // "ok" est désormais comptabilisée
        previewAfter.IgnoredCount.Should().Be(2);
    }

    [Fact]
    public async Task Worker_processes_pending_item_end_to_end_and_records_result_batch()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-WORKER-1");
        var item = await ctx.Queue.EnqueueManualAsync(new EnqueueManualGenerationDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        var entity = ctx.Db.AccountingGenerationQueues.Single(q => q.Id == item.Id);
        var audit = Substitute.For<IAuditLogger>();

        await AccountingWorker.ProcessOneAsync(entity, ctx.Db, ctx.Engine, audit, ctx.Clock, CancellationToken.None);

        entity.Status.Should().Be(QueueStatus.COMPLETED);
        entity.ResultGenerationId.Should().NotBeNull();
        entity.StartedDate.Should().NotBeNull();
        entity.CompletedDate.Should().NotBeNull();
        ctx.Db.AccountingEntries.Should().HaveCount(2);
    }

    [Fact]
    public async Task Worker_marks_failed_when_settings_not_configured()
    {
        var ctx = BuildContext(isConfigured: false);
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-WORKER-FAIL");

        // Contourne EnqueueManualAsync (qui bloquerait si non configuré) pour simuler un item déjà en file.
        var entity = new AccountingGenerationQueue
        {
            CreatedDate = ctx.Clock.UtcNow, RequestedBy = 1, GenerationMode = AccountingGenerationMode.MANUAL,
            StartDate = ctx.Clock.UtcNow.AddDays(-1), EndDate = ctx.Clock.UtcNow.AddDays(1), Status = QueueStatus.PENDING
        };
        ctx.Db.AccountingGenerationQueues.Add(entity);
        ctx.Db.SaveChanges();
        var audit = Substitute.For<IAuditLogger>();

        await AccountingWorker.ProcessOneAsync(entity, ctx.Db, ctx.Engine, audit, ctx.Clock, CancellationToken.None);

        entity.Status.Should().Be(QueueStatus.FAILED);
        entity.Remarks.Should().NotBeNullOrEmpty();
        entity.ResultGenerationId.Should().BeNull();
    }

    [Fact]
    public async Task Worker_processes_two_queue_items_sequentially_without_double_counting_operations()
    {
        var ctx = BuildContext();
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 1000m, ctx.Clock.UtcNow, "OP-CONC-1");
        MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 2000m, ctx.Clock.UtcNow, "OP-CONC-2");

        var range = new GenerateAccountingEntriesDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null);
        var item1 = ctx.Db.AccountingGenerationQueues.Add(new AccountingGenerationQueue
        {
            CreatedDate = ctx.Clock.UtcNow, RequestedBy = 1, GenerationMode = AccountingGenerationMode.MANUAL,
            StartDate = range.StartDate, EndDate = range.EndDate, Status = QueueStatus.PENDING
        }).Entity;
        var item2 = ctx.Db.AccountingGenerationQueues.Add(new AccountingGenerationQueue
        {
            CreatedDate = ctx.Clock.UtcNow, RequestedBy = 1, GenerationMode = AccountingGenerationMode.MANUAL,
            StartDate = range.StartDate, EndDate = range.EndDate, Status = QueueStatus.PENDING
        }).Entity;
        ctx.Db.SaveChanges();
        var audit = Substitute.For<IAuditLogger>();

        await AccountingWorker.ProcessPendingAsync(ctx.Db, ctx.Engine, audit, ctx.Clock, CancellationToken.None);

        item1.Status.Should().Be(QueueStatus.COMPLETED);
        item2.Status.Should().Be(QueueStatus.COMPLETED);
        // 2 opérations x 2 lignes (détaillé) = 4 écritures au total, jamais dupliquées entre les deux items de la file.
        ctx.Db.AccountingEntries.Should().HaveCount(4);
        ctx.Db.AccountingEntries.Select(e => e.CashOperationId).Where(id => id != null).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task Worker_processes_moderate_batch_within_reasonable_time()
    {
        var ctx = BuildContext();
        for (var i = 1; i <= 50; i++)
            MakeOperation(ctx.Db, ctx.Session, OperationDirection.IN, 100, 10m * i, ctx.Clock.UtcNow, $"OP-PERF-{i}");

        var item = await ctx.Queue.EnqueueManualAsync(new EnqueueManualGenerationDto(ctx.Clock.UtcNow.AddDays(-1), ctx.Clock.UtcNow.AddDays(1), null));
        var entity = ctx.Db.AccountingGenerationQueues.Single(q => q.Id == item.Id);
        var audit = Substitute.For<IAuditLogger>();

        var sw = Stopwatch.StartNew();
        await AccountingWorker.ProcessOneAsync(entity, ctx.Db, ctx.Engine, audit, ctx.Clock, CancellationToken.None);
        sw.Stop();

        entity.Status.Should().Be(QueueStatus.COMPLETED);
        ctx.Db.AccountingEntries.Should().HaveCount(100); // 50 opérations x 2 lignes
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }
}
