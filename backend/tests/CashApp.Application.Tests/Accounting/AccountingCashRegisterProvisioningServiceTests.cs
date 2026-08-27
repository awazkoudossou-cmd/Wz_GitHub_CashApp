using CashApp.Application.Accounting;
using CashApp.Application.CashRegisters;
using CashApp.Application.CashRegisters.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Tests.Fakes;
using CashApp.Application.Tests.Infrastructure;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;
using CashApp.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace CashApp.Application.Tests.Accounting;

public class AccountingCashRegisterProvisioningServiceTests
{
    private sealed record Context(AppDbContext Db, FakeClock Clock, IFeatureService Features);

    private static Context BuildContext(bool featureEnabled = true)
    {
        var (db, clock) = TestDbContextFactory.Create();
        TestDbContextFactory.SeedMinimalAsync(db).GetAwaiter().GetResult();

        var features = Substitute.For<IFeatureService>();
        features.IsEnabledAsync(FeatureCodes.AdvAccounting, Arg.Any<CancellationToken>()).Returns(featureEnabled);

        return new Context(db, clock, features);
    }

    private static CashRegisterService BuildService(Context ctx)
    {
        var audit = Substitute.For<IAuditLogger>();
        var provisioning = new AccountingCashRegisterProvisioningService(ctx.Db, ctx.Features, audit);
        return new CashRegisterService(ctx.Db, ctx.Clock, provisioning);
    }

    private static CreateCashRegisterDto MakeDto(string code, string name) =>
        new(code, name, null, "XOF", OperationDirection.IN, PaymentMethod.CASH);

    [Fact]
    public async Task CreateAsync_does_not_assign_accounting_when_feature_disabled()
    {
        var ctx = BuildContext(featureEnabled: false);
        ctx.Db.AccountingSettings.Add(new AccountingSettings
        {
            CashAccountRootNumber = "571100", CashAccountNumberLength = 8, CashJournalRootCode = "CAI"
        });
        await ctx.Db.SaveChangesAsync();
        var service = BuildService(ctx);

        var result = await service.CreateAsync(MakeDto("C2", "Caisse Annexe"));

        result.AccountingAccountId.Should().BeNull();
        result.AccountingJournalId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_does_not_assign_accounting_when_numbering_not_configured()
    {
        var ctx = BuildContext(featureEnabled: true);
        var service = BuildService(ctx);

        var result = await service.CreateAsync(MakeDto("C3", "Caisse Sud"));

        result.AccountingAccountId.Should().BeNull();
        result.AccountingJournalId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_assigns_auto_generated_account_and_journal_with_expected_format()
    {
        var ctx = BuildContext(featureEnabled: true);
        ctx.Db.AccountingSettings.Add(new AccountingSettings
        {
            CashAccountRootNumber = "571100", CashAccountNumberLength = 8, CashJournalRootCode = "CAI"
        });
        await ctx.Db.SaveChangesAsync();
        var service = BuildService(ctx);

        var result = await service.CreateAsync(MakeDto("C4", "Caisse Principale"));

        result.AccountingAccountNumber.Should().Be("57110001");
        result.AccountingJournalCode.Should().Be("CAI001");

        var account = await ctx.Db.AccountingAccounts.SingleAsync(a => a.Id == result.AccountingAccountId);
        account.Nature.Should().Be(AccountingAccountNature.CASH);
        account.Name.Should().Be("Caisse Principale");
        account.IsActive.Should().BeTrue();

        var journal = await ctx.Db.AccountingJournals.SingleAsync(j => j.Id == result.AccountingJournalId);
        journal.Name.Should().Be("CAI001 - Caisse Principale");
    }

    [Fact]
    public async Task CreateAsync_uses_default_suffix_width_when_length_not_configured()
    {
        var ctx = BuildContext(featureEnabled: true);
        ctx.Db.AccountingSettings.Add(new AccountingSettings
        {
            CashAccountRootNumber = "5711", CashAccountNumberLength = null, CashJournalRootCode = "CAI"
        });
        await ctx.Db.SaveChangesAsync();
        var service = BuildService(ctx);

        var result = await service.CreateAsync(MakeDto("C9", "Caisse Défaut"));

        result.AccountingAccountNumber.Should().Be("571101"); // racine (4) + suffixe 2 chiffres par défaut
    }

    [Fact]
    public async Task CreateAsync_increments_sequence_across_multiple_registers()
    {
        var ctx = BuildContext(featureEnabled: true);
        ctx.Db.AccountingSettings.Add(new AccountingSettings
        {
            CashAccountRootNumber = "571100", CashAccountNumberLength = 8, CashJournalRootCode = "CAI"
        });
        await ctx.Db.SaveChangesAsync();
        var service = BuildService(ctx);

        var first = await service.CreateAsync(MakeDto("C5", "Caisse A"));
        var second = await service.CreateAsync(MakeDto("C6", "Caisse B"));

        first.AccountingAccountNumber.Should().Be("57110001");
        second.AccountingAccountNumber.Should().Be("57110002");
        first.AccountingJournalCode.Should().Be("CAI001");
        second.AccountingJournalCode.Should().Be("CAI002");
    }

    [Fact]
    public async Task Sequence_is_never_reused_even_after_account_journal_and_register_deletion()
    {
        var ctx = BuildContext(featureEnabled: true);
        ctx.Db.AccountingSettings.Add(new AccountingSettings
        {
            CashAccountRootNumber = "571100", CashAccountNumberLength = 8, CashJournalRootCode = "CAI"
        });
        await ctx.Db.SaveChangesAsync();
        var service = BuildService(ctx);

        var first = await service.CreateAsync(MakeDto("C7", "Caisse Temp"));

        ctx.Db.AccountingAccounts.Remove(await ctx.Db.AccountingAccounts.SingleAsync(a => a.Id == first.AccountingAccountId));
        ctx.Db.AccountingJournals.Remove(await ctx.Db.AccountingJournals.SingleAsync(j => j.Id == first.AccountingJournalId));
        ctx.Db.CashRegisters.Remove(await ctx.Db.CashRegisters.SingleAsync(r => r.Id == first.Id));
        await ctx.Db.SaveChangesAsync();

        var second = await service.CreateAsync(MakeDto("C8", "Caisse Nouvelle"));

        second.AccountingAccountNumber.Should().Be("57110002");
        second.AccountingJournalCode.Should().Be("CAI002");
    }
}
