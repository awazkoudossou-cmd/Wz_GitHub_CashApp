using CashApp.Domain.Entities.V2;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

// === Comptabilité (V3) ===

public class AccountingJournalConfiguration : IEntityTypeConfiguration<AccountingJournal>
{
    public void Configure(EntityTypeBuilder<AccountingJournal> b)
    {
        b.ToTable("accounting_journals");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.IsActive).IsRequired();
    }
}

public class AccountingAccountConfiguration : IEntityTypeConfiguration<AccountingAccount>
{
    public void Configure(EntityTypeBuilder<AccountingAccount> b)
    {
        b.ToTable("accounting_accounts");
        b.HasKey(x => x.Id);
        b.Property(x => x.AccountNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.AccountNumber).IsUnique();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Nature).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.HasIndex(x => x.Nature);
    }
}

public class AccountingSettingsConfiguration : IEntityTypeConfiguration<AccountingSettings>
{
    public void Configure(EntityTypeBuilder<AccountingSettings> b)
    {
        b.ToTable("accounting_settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.GenerationType).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.GenerationMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.NarrationTemplate).HasMaxLength(500);
        b.Property(x => x.IsConfigured).IsRequired();
        b.Property(x => x.CashAccountRootNumber).HasMaxLength(20);
        b.Property(x => x.CashJournalRootCode).HasMaxLength(20);
    }
}

public class AccountingGenerationConfiguration : IEntityTypeConfiguration<AccountingGeneration>
{
    public void Configure(EntityTypeBuilder<AccountingGeneration> b)
    {
        b.ToTable("accounting_generations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.Reference).IsUnique();
        b.Property(x => x.GenerationType).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.GenerationMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Remarks).HasMaxLength(1000);

        b.HasOne(x => x.GeneratedByUser).WithMany().HasForeignKey(x => x.GeneratedBy).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.StartDate, x.EndDate });
    }
}

public class AccountingGenerationLogConfiguration : IEntityTypeConfiguration<AccountingGenerationLog>
{
    public void Configure(EntityTypeBuilder<AccountingGenerationLog> b)
    {
        b.ToTable("accounting_generation_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.ErrorsJson).HasColumnType("TEXT");

        b.HasOne(x => x.Generation).WithMany().HasForeignKey(x => x.GenerationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.PerformedByUser).WithMany().HasForeignKey(x => x.PerformedBy).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.GenerationId);
        b.HasIndex(x => x.PerformedAt);
    }
}

public class AccountingGenerationQueueConfiguration : IEntityTypeConfiguration<AccountingGenerationQueue>
{
    public void Configure(EntityTypeBuilder<AccountingGenerationQueue> b)
    {
        b.ToTable("accounting_generation_queues");
        b.HasKey(x => x.Id);
        b.Property(x => x.GenerationMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.CashRegisterIdsJson).HasColumnType("TEXT");
        b.Property(x => x.Remarks).HasMaxLength(1000);

        b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ResultGeneration).WithMany().HasForeignKey(x => x.ResultGenerationId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedDate);
    }
}

public class AccountingExportLogConfiguration : IEntityTypeConfiguration<AccountingExportLog>
{
    public void Configure(EntityTypeBuilder<AccountingExportLog> b)
    {
        b.ToTable("accounting_export_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExportNumber).HasMaxLength(60).IsRequired();
        b.Property(x => x.ExportType).HasMaxLength(20).IsRequired();
        b.Property(x => x.GenerationType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.GenerationMode).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        b.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.FilterJson).HasColumnType("TEXT");
        b.Property(x => x.Remarks).HasMaxLength(1000);

        b.HasOne(x => x.Generation).WithMany().HasForeignKey(x => x.GenerationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ExportedByUser).WithMany().HasForeignKey(x => x.ExportedBy).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.ExportNumber).IsUnique();
        b.HasIndex(x => x.ExportedAt);
        b.HasIndex(x => x.Status);
    }
}

public class AccountingEntryConfiguration : IEntityTypeConfiguration<AccountingEntry>
{
    public void Configure(EntityTypeBuilder<AccountingEntry> b)
    {
        b.ToTable("accounting_entries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(60).IsRequired();
        b.Property(x => x.PieceNumber).HasMaxLength(60);
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.Comment).HasMaxLength(2000);
        b.Property(x => x.Debit).HasColumnType("decimal(18,2)");
        b.Property(x => x.Credit).HasColumnType("decimal(18,2)");

        b.HasOne(x => x.Generation).WithMany(g => g.Entries).HasForeignKey(x => x.GenerationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.CashOperation).WithMany().HasForeignKey(x => x.CashOperationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Journal).WithMany().HasForeignKey(x => x.JournalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.GenerationId);
        b.HasIndex(x => x.CashOperationId);
        b.HasIndex(x => x.JournalId);
        b.HasIndex(x => x.AccountId);
        b.HasIndex(x => x.OperationDate);
    }
}

public class AccountingPendingConfiguration : IEntityTypeConfiguration<AccountingPending>
{
    public void Configure(EntityTypeBuilder<AccountingPending> b)
    {
        b.ToTable("accounting_pendings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();

        b.HasOne(x => x.CashOperation).WithMany().HasForeignKey(x => x.CashOperationId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.CashOperationId);
        b.HasIndex(x => x.Resolved);
    }
}
