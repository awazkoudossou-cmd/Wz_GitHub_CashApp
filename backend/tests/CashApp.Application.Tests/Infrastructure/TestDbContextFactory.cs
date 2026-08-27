using CashApp.Application.Common.Interfaces;
using CashApp.Application.Tests.Fakes;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using CashApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Tests.Infrastructure;

public static class TestDbContextFactory
{
    public static (AppDbContext Db, FakeClock Clock) Create()
    {
        var clock = new FakeClock();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"cashapp-test-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new AppDbContext(options, clock);
        db.Database.EnsureCreated();
        return (db, clock);
    }

    public static async Task SeedMinimalAsync(AppDbContext db)
    {
        db.Users.Add(new User
        {
            Id = 1, Username = "admin", FullName = "Admin",
            PasswordHash = "x", RoleCode = RoleCodes.Admin, IsActive = true
        });
        db.CashRegisters.Add(new CashRegister
        {
            Id = 10, Code = "C1", Name = "Caisse 1", CurrencyCode = "XOF", IsActive = true
        });
        db.Categories.AddRange(
            new Category { Id = 100, Code = "SALE", Label = "Vente", Direction = OperationDirection.IN, IsActive = true },
            new Category { Id = 101, Code = "PURCHASE", Label = "Achat", Direction = OperationDirection.OUT, IsActive = true }
        );
        db.UserCashRegisters.Add(new UserCashRegister { UserId = 1, CashRegisterId = 10, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
}
