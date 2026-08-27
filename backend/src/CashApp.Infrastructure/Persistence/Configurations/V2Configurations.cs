using CashApp.Domain.Entities.V2;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashApp.Infrastructure.Persistence.Configurations;

// === Approval ===

public class ApprovalRuleConfiguration : IEntityTypeConfiguration<ApprovalRule>
{
    public void Configure(EntityTypeBuilder<ApprovalRule> b)
    {
        b.ToTable("approval_rules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.AmountThreshold).HasColumnType("decimal(18,2)");
        b.Property(x => x.CurrencyCode).HasMaxLength(8);
        b.Property(x => x.RequiredApproverRole).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.TargetType);
    }
}

public class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> b)
    {
        b.ToTable("approval_requests");
        b.HasKey(x => x.Id);
        b.Property(x => x.RequestRef).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.RequestRef).IsUnique();
        b.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.TargetEntityType).HasMaxLength(80).IsRequired();
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.CurrencyCode).HasMaxLength(8);
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.DecisionComment).HasMaxLength(1000);

        b.HasOne(x => x.ApprovalRule).WithMany().HasForeignKey(x => x.ApprovalRuleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CashRegister).WithMany().HasForeignKey(x => x.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedBy).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.TargetEntityType, x.TargetEntityId });
        b.HasIndex(x => x.CashRegisterId);
    }
}

public class ApprovalActionConfiguration : IEntityTypeConfiguration<ApprovalAction>
{
    public void Configure(EntityTypeBuilder<ApprovalAction> b)
    {
        b.ToTable("approval_actions");
        b.HasKey(x => x.Id);
        b.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Comment).HasMaxLength(1000);

        b.HasOne(x => x.ApprovalRequest).WithMany(p => p.Actions).HasForeignKey(x => x.ApprovalRequestId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.PerformedByUser).WithMany().HasForeignKey(x => x.PerformedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

// === CashTransfer ===

public class CashTransferConfiguration : IEntityTypeConfiguration<CashTransfer>
{
    public void Configure(EntityTypeBuilder<CashTransfer> b)
    {
        b.ToTable("cash_transfers");
        b.HasKey(x => x.Id);
        b.Property(x => x.TransferRef).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.TransferRef).IsUnique();
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        b.HasOne(x => x.SourceCashRegister).WithMany().HasForeignKey(x => x.SourceCashRegisterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationCashRegister).WithMany().HasForeignKey(x => x.DestinationCashRegisterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SourceSession).WithMany().HasForeignKey(x => x.SourceSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationSession).WithMany().HasForeignKey(x => x.DestinationSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SourceOperation).WithMany().HasForeignKey(x => x.SourceOperationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.DestinationOperation).WithMany().HasForeignKey(x => x.DestinationOperationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ApprovalRequest).WithMany().HasForeignKey(x => x.ApprovalRequestId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.SourceCashRegisterId);
        b.HasIndex(x => x.DestinationCashRegisterId);
        b.HasIndex(x => x.TransferDate);
    }
}

// === BankDeposit ===

public class BankDepositConfiguration : IEntityTypeConfiguration<BankDeposit>
{
    public void Configure(EntityTypeBuilder<BankDeposit> b)
    {
        b.ToTable("bank_deposits");
        b.HasKey(x => x.Id);
        b.Property(x => x.DepositRef).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.DepositRef).IsUnique();
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        b.Property(x => x.BankName).HasMaxLength(150).IsRequired();
        b.Property(x => x.AccountReference).HasMaxLength(80);
        b.Property(x => x.DepositSlipReference).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        b.HasOne(x => x.CashRegister).WithMany().HasForeignKey(x => x.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CashSession).WithMany().HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.LinkedOperation).WithMany().HasForeignKey(x => x.LinkedOperationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ApprovalRequest).WithMany().HasForeignKey(x => x.ApprovalRequestId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CashRegisterId);
        b.HasIndex(x => x.DepositDate);
    }
}

// === Anomaly ===

public class AnomalyCaseConfiguration : IEntityTypeConfiguration<AnomalyCase>
{
    public void Configure(EntityTypeBuilder<AnomalyCase> b)
    {
        b.ToTable("anomaly_cases");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.Reference).IsUnique();
        b.Property(x => x.RelatedEntityType).HasMaxLength(80);
        b.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.ResolutionComment).HasMaxLength(2000);

        b.HasOne(x => x.CashRegister).WithMany().HasForeignKey(x => x.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CashSession).WithMany().HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DetectedByUser).WithMany().HasForeignKey(x => x.DetectedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedTo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ResolvedByUser).WithMany().HasForeignKey(x => x.ResolvedBy).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.Severity);
        b.HasIndex(x => x.CashRegisterId);
        b.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });
    }
}

public class AnomalyCommentConfiguration : IEntityTypeConfiguration<AnomalyComment>
{
    public void Configure(EntityTypeBuilder<AnomalyComment> b)
    {
        b.ToTable("anomaly_comments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();

        b.HasOne(x => x.AnomalyCase).WithMany(p => p.Comments).HasForeignKey(x => x.AnomalyCaseId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AuthorUser).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}

// === Variance ===

public class VarianceCaseConfiguration : IEntityTypeConfiguration<VarianceCase>
{
    public void Configure(EntityTypeBuilder<VarianceCase> b)
    {
        b.ToTable("variance_cases");
        b.HasKey(x => x.Id);
        b.Property(x => x.VarianceAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        b.HasOne(x => x.CashSession).WithMany().HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ApprovalRequest).WithMany().HasForeignKey(x => x.ApprovalRequestId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.AnomalyCase).WithMany().HasForeignKey(x => x.AnomalyCaseId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.CashSessionId);
        b.HasIndex(x => x.Status);
    }
}

public class VarianceJustificationConfiguration : IEntityTypeConfiguration<VarianceJustification>
{
    public void Configure(EntityTypeBuilder<VarianceJustification> b)
    {
        b.ToTable("variance_justifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Comment).HasMaxLength(2000).IsRequired();

        b.HasOne(x => x.VarianceCase).WithMany(p => p.Justifications).HasForeignKey(x => x.VarianceCaseId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ProvidedByUser).WithMany().HasForeignKey(x => x.ProvidedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VarianceActionConfiguration : IEntityTypeConfiguration<VarianceAction>
{
    public void Configure(EntityTypeBuilder<VarianceAction> b)
    {
        b.ToTable("variance_actions");
        b.HasKey(x => x.Id);
        b.Property(x => x.ActionType).HasMaxLength(30).IsRequired();
        b.Property(x => x.Comment).HasMaxLength(2000);

        b.HasOne(x => x.VarianceCase).WithMany(p => p.Actions).HasForeignKey(x => x.VarianceCaseId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.PerformedByUser).WithMany().HasForeignKey(x => x.PerformedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

// === AuditLog ===

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.OldValuesJson).HasColumnType("TEXT");
        b.Property(x => x.NewValuesJson).HasColumnType("TEXT");
        b.Property(x => x.MetadataJson).HasColumnType("TEXT");
        b.Property(x => x.IpAddress).HasMaxLength(45);

        b.HasOne(x => x.PerformedByUser).WithMany().HasForeignKey(x => x.PerformedBy).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.PerformedAt);
        b.HasIndex(x => new { x.EntityType, x.EntityId });
        b.HasIndex(x => x.ActionType);
        b.HasIndex(x => x.PerformedBy);
    }
}

// === V2-B ===

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> b)
    {
        b.ToTable("attachments");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        b.Property(x => x.FileName).HasMaxLength(200).IsRequired();
        b.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.StoredPath).HasMaxLength(500).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);

        b.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.EntityType, x.EntityId });
        b.HasIndex(x => x.IsDeleted);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> b)
    {
        b.ToTable("import_batches");
        b.HasKey(x => x.Id);
        b.Property(x => x.BatchRef).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.BatchRef).IsUnique();
        b.Property(x => x.BatchType).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.OriginalFileName).HasMaxLength(255);
        b.Property(x => x.StoredPath).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.ErrorSummaryJson).HasColumnType("TEXT");

        b.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CashRegister).WithMany().HasForeignKey(x => x.CashRegisterId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Status);
    }
}

public class ImportBatchLineConfiguration : IEntityTypeConfiguration<ImportBatchLine>
{
    public void Configure(EntityTypeBuilder<ImportBatchLine> b)
    {
        b.ToTable("import_batch_lines");
        b.HasKey(x => x.Id);
        b.Property(x => x.RawDataJson).HasColumnType("TEXT").IsRequired();
        b.Property(x => x.ParsedDataJson).HasColumnType("TEXT");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.ErrorMessage).HasMaxLength(1000);
        b.Property(x => x.CreatedEntityType).HasMaxLength(80);

        b.HasOne(x => x.ImportBatch).WithMany(p => p.Lines).HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.ImportBatchId);
        b.HasIndex(x => x.Status);
    }
}

public class ReconciliationBatchConfiguration : IEntityTypeConfiguration<ReconciliationBatch>
{
    public void Configure(EntityTypeBuilder<ReconciliationBatch> b)
    {
        b.ToTable("reconciliation_batches");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.Reference).IsUnique();
        b.Property(x => x.BatchType).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasOne(x => x.CashRegister).WithMany().HasForeignKey(x => x.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReconciliationItemConfiguration : IEntityTypeConfiguration<ReconciliationItem>
{
    public void Configure(EntityTypeBuilder<ReconciliationItem> b)
    {
        b.ToTable("reconciliation_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.LeftEntityType).HasMaxLength(80).IsRequired();
        b.Property(x => x.RightEntityType).HasMaxLength(80);
        b.Property(x => x.MatchedAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.MatchStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.ExternalReference).HasMaxLength(100);
        b.Property(x => x.ExternalAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.MatchComment).HasMaxLength(1000);

        b.HasOne(x => x.ReconciliationBatch).WithMany(p => p.Items).HasForeignKey(x => x.ReconciliationBatchId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.CashOperation).WithMany().HasForeignKey(x => x.CashOperationId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.LeftEntityType, x.LeftEntityId });
        b.HasIndex(x => new { x.RightEntityType, x.RightEntityId });
    }
}
