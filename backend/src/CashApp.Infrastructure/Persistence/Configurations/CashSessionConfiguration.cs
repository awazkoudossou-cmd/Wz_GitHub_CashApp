using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> b)
    {
        b.ToTable("cash_sessions");
        b.HasKey(x => x.Id);

        b.Property(x => x.OpenedAt).IsRequired();
        b.Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)").IsRequired();
        b.Property(x => x.TheoreticalBalance).HasColumnType("decimal(18,2)");
        b.Property(x => x.PhysicalBalance).HasColumnType("decimal(18,2)");
        b.Property(x => x.VarianceAmount).HasColumnType("decimal(18,2)");

        b.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(15)
            .IsRequired();

        b.Property(x => x.OpenComment).HasMaxLength(1000);
        b.Property(x => x.CloseComment).HasMaxLength(1000);

        b.HasOne(x => x.CashRegister)
            .WithMany(c => c.CashSessions)
            .HasForeignKey(x => x.CashRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.OpenedByUser)
            .WithMany()
            .HasForeignKey(x => x.OpenedBy)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ClosedByUser)
            .WithMany()
            .HasForeignKey(x => x.ClosedBy)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.CashRegisterId);
        b.HasIndex(x => x.Status);
    }
}
