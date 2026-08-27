using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

public class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> b)
    {
        b.ToTable("cash_registers");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();

        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        b.Property(x => x.IsActive).IsRequired();

        b.Property(x => x.DefaultDirection)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();
        b.Property(x => x.DefaultPaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.HasOne(x => x.AccountingJournal)
            .WithMany()
            .HasForeignKey(x => x.AccountingJournalId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.AccountingAccount)
            .WithMany()
            .HasForeignKey(x => x.AccountingAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
