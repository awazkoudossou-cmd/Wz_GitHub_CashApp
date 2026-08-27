using CashApp.Application.Auth;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Application.Settings.Dtos;
using CashApp.Application.Tests.Infrastructure;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CashApp.Application.Tests.Auth;

public class AuthServiceTests
{
    private static AuthService BuildService(CashApp.Infrastructure.Persistence.AppDbContext db)
    {
        var hasher = Substitute.For<IPasswordHasher>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var settings = Substitute.For<ISettingsService>();
        settings.GetAppModeAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new AppModeDto(AppMode.ADVANCED)));
        return new AuthService(db, hasher, jwt, settings);
    }

    private static User AddUser(CashApp.Infrastructure.Persistence.AppDbContext db, string username, string role)
    {
        var user = new User
        {
            Username = username, FullName = username, PasswordHash = "x", RoleCode = role, IsActive = true
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task Supervisor_without_explicit_assignment_sees_all_active_cash_registers()
    {
        var (db, _) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        db.CashRegisters.Add(new CashRegister { Code = "C2", Name = "Caisse 2", CurrencyCode = "XOF", IsActive = true });
        db.SaveChanges();
        var supervisor = AddUser(db, "supervisor1", RoleCodes.Supervisor);

        var service = BuildService(db);
        var context = await service.GetCurrentContextAsync(supervisor.Id);

        context.CashRegisters.Should().HaveCount(2);
        context.CashRegisters.Select(r => r.Code).Should().Contain(new[] { "C1", "C2" });
    }

    [Fact]
    public async Task Admin_without_explicit_assignment_sees_all_active_cash_registers()
    {
        var (db, _) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        var admin = db.Users.Single(u => u.Username == "admin");

        var service = BuildService(db);
        var context = await service.GetCurrentContextAsync(admin.Id);

        context.CashRegisters.Should().ContainSingle(r => r.Code == "C1");
    }

    [Fact]
    public async Task Cashier_without_explicit_assignment_sees_no_cash_registers()
    {
        var (db, _) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        var cashier = AddUser(db, "cashier1", RoleCodes.Cashier);

        var service = BuildService(db);
        var context = await service.GetCurrentContextAsync(cashier.Id);

        context.CashRegisters.Should().BeEmpty();
    }

    [Fact]
    public async Task Cashier_with_explicit_assignment_sees_only_assigned_cash_registers()
    {
        var (db, _) = TestDbContextFactory.Create();
        await TestDbContextFactory.SeedMinimalAsync(db);
        db.CashRegisters.Add(new CashRegister { Id = 20, Code = "C2", Name = "Caisse 2", CurrencyCode = "XOF", IsActive = true });
        db.SaveChanges();
        var cashier = AddUser(db, "cashier2", RoleCodes.Cashier);
        db.UserCashRegisters.Add(new UserCashRegister { UserId = cashier.Id, CashRegisterId = 20, AssignedAt = DateTime.UtcNow });
        db.SaveChanges();

        var service = BuildService(db);
        var context = await service.GetCurrentContextAsync(cashier.Id);

        context.CashRegisters.Should().ContainSingle(r => r.Code == "C2");
    }
}
