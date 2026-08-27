using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

public class CashOperationConfiguration : IEntityTypeConfiguration<CashOperation>
{
    public void Configure(EntityTypeBuilder<CashOperation> b)
    {
        b.ToTable("cash_operations");
        b.HasKey(x => x.Id);

        b.Property(x => x.OperationRef).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.OperationRef).IsUnique();

        b.Property(x => x.OperationDate).IsRequired();
        b.Property(x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        b.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();

        b.Property(x => x.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.Label).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.ExternalReference).HasMaxLength(100);
        b.Property(x => x.ThirdPartyName).HasMaxLength(200);
        b.Property(x => x.DeleteReason).HasMaxLength(500);

        b.HasOne(x => x.CashRegister)
            .WithMany(c => c.CashOperations)
            .HasForeignKey(x => x.CashRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CashSession)
            .WithMany(s => s.CashOperations)
            .HasForeignKey(x => x.CashSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.CashRegisterId);
        b.HasIndex(x => x.CashSessionId);
        b.HasIndex(x => x.OperationDate);
        b.HasIndex(x => x.IsDeleted);

        // Filtre global soft delete : par défaut on n'expose pas les opérations supprimées.
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
