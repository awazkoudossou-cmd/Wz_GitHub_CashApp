using System.Text;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Imports;
using CashApp.Application.Imports.Dtos;
using CashApp.Application.Imports.Parsers;
using CashApp.Application.Settings;
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

namespace CashApp.Application.Tests.Imports;

public class AccountingAccountImportParserTests
{
    private sealed record Context(AppDbContext Db, FakeClock Clock, ImportService Service);

    private static Context BuildContext()
    {
        var (db, clock) = TestDbContextFactory.Create();
        TestDbContextFactory.SeedMinimalAsync(db).GetAwaiter().GetResult();

        var settings = Substitute.For<ISettingsService>();
        var root = Path.Combine(Path.GetTempPath(), "cashapp-tests-imports", Guid.NewGuid().ToString("N"));
        settings.GetRawAsync(SettingKeys.ImportsRootPath, Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(root));
        settings.GetRawAsync(SettingKeys.ImportAllowPartialSuccess, Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>("true"));

        var user = new FakeCurrentUser();
        var audit = Substitute.For<IAuditLogger>();
        var parser = new AccountingAccountImportParser(db);
        var service = new ImportService(db, settings, user, clock, audit, new IImportParser[] { parser });

        return new Context(db, clock, service);
    }

    private static Stream ToCsvStream(string csv) => new MemoryStream(Encoding.UTF8.GetBytes(csv));

    [Fact]
    public async Task PreviewAsync_flags_duplicate_within_same_file()
    {
        var ctx = BuildContext();
        var csv = "account_number,name,nature\n571100,Caisse 1,CASH\n571100,Caisse 1 bis,CASH\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.ACCOUNTING_ACCOUNTS, null, "accounts.csv", ToCsvStream(csv));

        var preview = await ctx.Service.PreviewAsync(batch.Id);

        preview.ValidLines.Should().Be(1);
        preview.InvalidLines.Should().Be(1);
        preview.Lines[1].ErrorMessage.Should().Contain("plusieurs fois");
    }

    [Fact]
    public async Task PreviewAsync_flags_account_already_existing_in_chart_of_accounts()
    {
        var ctx = BuildContext();
        ctx.Db.AccountingAccounts.Add(new AccountingAccount { AccountNumber = "571100", Name = "Caisse existante", Nature = AccountingAccountNature.CASH, IsActive = true });
        await ctx.Db.SaveChangesAsync();

        var csv = "account_number,name,nature\n571100,Nouvelle caisse,CASH\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.ACCOUNTING_ACCOUNTS, null, "accounts.csv", ToCsvStream(csv));

        var preview = await ctx.Service.PreviewAsync(batch.Id);

        preview.InvalidLines.Should().Be(1);
        preview.Lines[0].ErrorMessage.Should().Contain("existe déjà");
    }

    [Fact]
    public async Task ValidateAsync_rejects_invalid_nature()
    {
        var ctx = BuildContext();
        var csv = "account_number,name,nature\n571300,Caisse invalide,INVALID_NATURE\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.ACCOUNTING_ACCOUNTS, null, "accounts.csv", ToCsvStream(csv));

        var preview = await ctx.Service.PreviewAsync(batch.Id);

        preview.InvalidLines.Should().Be(1);
        preview.Lines[0].ErrorMessage.Should().Contain("Nature");
    }

    [Fact]
    public async Task ConfirmAsync_creates_accounts_from_valid_lines()
    {
        var ctx = BuildContext();
        var csv = "account_number,name,nature,is_active\n571200,Caisse Nord,CASH,true\n701000,Ventes,REVENUE,\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.ACCOUNTING_ACCOUNTS, null, "accounts.csv", ToCsvStream(csv));
        await ctx.Service.PreviewAsync(batch.Id);

        var result = await ctx.Service.ConfirmAsync(batch.Id, new ConfirmImportDto(false));

        result.ImportedLines.Should().Be(2);
        ctx.Db.AccountingAccounts.Should().Contain(a => a.AccountNumber == "571200" && a.Nature == AccountingAccountNature.CASH);
        ctx.Db.AccountingAccounts.Should().Contain(a => a.AccountNumber == "701000" && a.Nature == AccountingAccountNature.REVENUE && a.IsActive);
    }

    [Fact]
    public async Task ConfirmAsync_rejects_line_if_account_was_created_manually_after_preview()
    {
        var ctx = BuildContext();
        var csv = "account_number,name,nature\n571400,Caisse Est,CASH\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.ACCOUNTING_ACCOUNTS, null, "accounts.csv", ToCsvStream(csv));
        await ctx.Service.PreviewAsync(batch.Id);

        // Le compte est créé manuellement entre la prévisualisation et la confirmation.
        ctx.Db.AccountingAccounts.Add(new AccountingAccount { AccountNumber = "571400", Name = "Créé entre-temps", Nature = AccountingAccountNature.CASH, IsActive = true });
        await ctx.Db.SaveChangesAsync();

        var result = await ctx.Service.ConfirmAsync(batch.Id, new ConfirmImportDto(true));

        result.ImportedLines.Should().Be(0);
        ctx.Db.AccountingAccounts.Count(a => a.AccountNumber == "571400").Should().Be(1);
    }
}
