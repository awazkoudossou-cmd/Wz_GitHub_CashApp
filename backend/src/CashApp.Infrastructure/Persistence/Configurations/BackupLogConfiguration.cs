using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

public class BackupLogConfiguration : IEntityTypeConfiguration<BackupLog>
{
    public void Configure(EntityTypeBuilder<BackupLog> b)
    {
        b.ToTable("backup_logs");
        b.HasKey(x => x.Id);

        b.Property(x => x.FileName).HasMaxLength(200).IsRequired();
        b.Property(x => x.FilePath).HasMaxLength(500).IsRequired();

        b.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
