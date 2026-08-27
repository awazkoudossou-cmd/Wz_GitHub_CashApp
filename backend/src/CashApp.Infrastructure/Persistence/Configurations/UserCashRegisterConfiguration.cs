using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

public class UserCashRegisterConfiguration : IEntityTypeConfiguration<UserCashRegister>
{
    public void Configure(EntityTypeBuilder<UserCashRegister> b)
    {
        b.ToTable("user_cash_registers");
        b.HasKey(x => new { x.UserId, x.CashRegisterId });

        b.HasIndex(x => new { x.UserId, x.CashRegisterId }).IsUnique();

        b.HasOne(x => x.User)
            .WithMany(u => u.UserCashRegisters)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.CashRegister)
            .WithMany(c => c.UserCashRegisters)
            .HasForeignKey(x => x.CashRegisterId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Property(x => x.AssignedAt).IsRequired();
    }
}
