using System.Text;
using CashApp.Application.CategoryGroups;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Imports;
using CashApp.Application.Imports.Dtos;
using CashApp.Application.Imports.Parsers;
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

namespace CashApp.Application.Tests.Imports;

public class CategoryImportParserTests
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
        var groups = new CategoryGroupService(db);
        var parser = new CategoryImportParser(db, groups);
        var service = new ImportService(db, settings, user, clock, audit, new IImportParser[] { parser });

        return new Context(db, clock, service);
    }

    private static Stream ToCsvStream(string csv) => new MemoryStream(Encoding.UTF8.GetBytes(csv));

    [Fact]
    public async Task PreviewAsync_flags_duplicate_within_same_file()
    {
        var ctx = BuildContext();
        var csv = "code,label,direction\nTRAVEL,Frais de déplacement,OUT\nTRAVEL,Doublon,OUT\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.CATEGORIES, null, "categories.csv", ToCsvStream(csv));

        var preview = await ctx.Service.PreviewAsync(batch.Id);

        preview.ValidLines.Should().Be(1);
        preview.InvalidLines.Should().Be(1);
        preview.Lines[1].ErrorMessage.Should().Contain("plusieurs fois");
    }

    [Fact]
    public async Task PreviewAsync_flags_category_already_existing()
    {
        var ctx = BuildContext();
        // "SALE" est déjà seedée par TestDbContextFactory.SeedMinimalAsync.
        var csv = "code,label,direction\nSALE,Doublon de vente,IN\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.CATEGORIES, null, "categories.csv", ToCsvStream(csv));

        var preview = await ctx.Service.PreviewAsync(batch.Id);

        preview.InvalidLines.Should().Be(1);
        preview.Lines[0].ErrorMessage.Should().Contain("existe déjà");
    }

    [Fact]
    public async Task ValidateAsync_rejects_invalid_direction()
    {
        var ctx = BuildContext();
        var csv = "code,label,direction\nMISC,Divers,SIDEWAYS\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.CATEGORIES, null, "categories.csv", ToCsvStream(csv));

        var preview = await ctx.Service.PreviewAsync(batch.Id);

        preview.InvalidLines.Should().Be(1);
        preview.Lines[0].ErrorMessage.Should().Contain("Direction");
    }

    [Fact]
    public async Task ConfirmAsync_creates_categories_and_resolves_group_by_name()
    {
        var ctx = BuildContext();
        var csv = "code,label,direction,group\nTRAVEL,Frais de déplacement,OUT,Charges diverses\nMISC,Divers,OUT,\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.CATEGORIES, null, "categories.csv", ToCsvStream(csv));
        await ctx.Service.PreviewAsync(batch.Id);

        var result = await ctx.Service.ConfirmAsync(batch.Id, new ConfirmImportDto(false));

        result.ImportedLines.Should().Be(2);

        var travel = await ctx.Db.Categories.Include(c => c.Group).SingleAsync(c => c.Code == "TRAVEL");
        travel.Direction.Should().Be(OperationDirection.OUT);
        travel.Group!.Name.Should().Be("Charges diverses");

        var misc = await ctx.Db.Categories.Include(c => c.Group).SingleAsync(c => c.Code == "MISC");
        misc.Group!.Name.Should().Be("Non classé");
        misc.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmAsync_rejects_line_if_category_was_created_manually_after_preview()
    {
        var ctx = BuildContext();
        var csv = "code,label,direction\nTRAVEL,Frais de déplacement,OUT\n";
        var batch = await ctx.Service.UploadAsync(ImportBatchType.CATEGORIES, null, "categories.csv", ToCsvStream(csv));
        await ctx.Service.PreviewAsync(batch.Id);

        ctx.Db.Categories.Add(new Category { Code = "TRAVEL", Label = "Créée entre-temps", Direction = OperationDirection.OUT, IsActive = true });
        await ctx.Db.SaveChangesAsync();

        var result = await ctx.Service.ConfirmAsync(batch.Id, new ConfirmImportDto(true));

        result.ImportedLines.Should().Be(0);
        ctx.Db.Categories.Count(c => c.Code == "TRAVEL").Should().Be(1);
    }
}
