using CashApp.Domain.Entities;
using CashApp.Domain.Entities.V2;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CashApp.Application.Common.Interfaces;

public interface IAppDbContext
{
    DatabaseFacade Database { get; }

    DbSet<User> Users { get; }
    DbSet<CashRegister> CashRegisters { get; }
    DbSet<UserCashRegister> UserCashRegisters { get; }
    DbSet<Category> Categories { get; }
    DbSet<CategoryGroup> CategoryGroups { get; }
    DbSet<ThirdParty> ThirdParties { get; }
    DbSet<CashSession> CashSessions { get; }
    DbSet<CashOperation> CashOperations { get; }
    DbSet<BackupLog> BackupLogs { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<FeatureSetting> FeatureSettings { get; }

    // V2-A
    DbSet<ApprovalRule> ApprovalRules { get; }
    DbSet<ApprovalRequest> ApprovalRequests { get; }
    DbSet<ApprovalAction> ApprovalActions { get; }
    DbSet<CashTransfer> CashTransfers { get; }
    DbSet<BankDeposit> BankDeposits { get; }
    DbSet<AnomalyCase> AnomalyCases { get; }
    DbSet<AnomalyComment> AnomalyComments { get; }
    DbSet<VarianceCase> VarianceCases { get; }
    DbSet<VarianceJustification> VarianceJustifications { get; }
    DbSet<VarianceAction> VarianceActions { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // V2-B (pré-câblé, services à venir)
    DbSet<Attachment> Attachments { get; }
    DbSet<ImportBatch> ImportBatches { get; }
    DbSet<ImportBatchLine> ImportBatchLines { get; }
    DbSet<ReconciliationBatch> ReconciliationBatches { get; }
    DbSet<ReconciliationItem> ReconciliationItems { get; }

    // V3 — Comptabilité
    DbSet<AccountingJournal> AccountingJournals { get; }
    DbSet<AccountingAccount> AccountingAccounts { get; }
    DbSet<AccountingSettings> AccountingSettings { get; }
    DbSet<AccountingGeneration> AccountingGenerations { get; }
    DbSet<AccountingEntry> AccountingEntries { get; }
    DbSet<AccountingPending> AccountingPendings { get; }
    DbSet<AccountingGenerationLog> AccountingGenerationLogs { get; }
    DbSet<AccountingGenerationQueue> AccountingGenerationQueues { get; }
    DbSet<AccountingExportLog> AccountingExportLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
