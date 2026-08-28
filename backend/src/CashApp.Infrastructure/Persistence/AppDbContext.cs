using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Common;
using CashApp.Domain.Entities;
using CashApp.Domain.Entities.V2;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly IDateTimeProvider? _clock;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public AppDbContext(DbContextOptions<AppDbContext> options, IDateTimeProvider clock) : base(options)
    {
        _clock = clock;
    }

    // V1
    public DbSet<User> Users => Set<User>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<UserCashRegister> UserCashRegisters => Set<UserCashRegister>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryGroup> CategoryGroups => Set<CategoryGroup>();
    public DbSet<ThirdParty> ThirdParties => Set<ThirdParty>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<CashOperation> CashOperations => Set<CashOperation>();
    public DbSet<BackupLog> BackupLogs => Set<BackupLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<FeatureSetting> FeatureSettings => Set<FeatureSetting>();

    // V2-A
    public DbSet<ApprovalRule> ApprovalRules => Set<ApprovalRule>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<CashTransfer> CashTransfers => Set<CashTransfer>();
    public DbSet<BankDeposit> BankDeposits => Set<BankDeposit>();
    public DbSet<AnomalyCase> AnomalyCases => Set<AnomalyCase>();
    public DbSet<AnomalyComment> AnomalyComments => Set<AnomalyComment>();
    public DbSet<VarianceCase> VarianceCases => Set<VarianceCase>();
    public DbSet<VarianceJustification> VarianceJustifications => Set<VarianceJustification>();
    public DbSet<VarianceAction> VarianceActions => Set<VarianceAction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // V2-B (pré-câblé)
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportBatchLine> ImportBatchLines => Set<ImportBatchLine>();
    public DbSet<ReconciliationBatch> ReconciliationBatches => Set<ReconciliationBatch>();
    public DbSet<ReconciliationItem> ReconciliationItems => Set<ReconciliationItem>();

    // V3 — Comptabilité
    public DbSet<AccountingJournal> AccountingJournals => Set<AccountingJournal>();
    public DbSet<AccountingAccount> AccountingAccounts => Set<AccountingAccount>();
    public DbSet<AccountingSettings> AccountingSettings => Set<AccountingSettings>();
    public DbSet<AccountingGeneration> AccountingGenerations => Set<AccountingGeneration>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();
    public DbSet<AccountingPending> AccountingPendings => Set<AccountingPending>();
    public DbSet<AccountingGenerationLog> AccountingGenerationLogs => Set<AccountingGenerationLog>();
    public DbSet<AccountingGenerationQueue> AccountingGenerationQueues => Set<AccountingGenerationQueue>();
    public DbSet<AccountingExportLog> AccountingExportLogs => Set<AccountingExportLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // "NOCASE" est une collation propre à SQLite (comparaison insensible à la casse
        // sur ThirdParty/CategoryGroup.Name) ; Postgres n'en dispose pas nativement et
        // rejette la création de table avec cette collation. On la retire donc hors SQLite
        // (comparaison/unicité redeviennent sensibles à la casse sur Postgres).
        if (!Database.IsSqlite())
        {
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
            {
                if (property.GetCollation() == "NOCASE")
                {
                    property.SetCollation(null);
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock?.UtcNow ?? DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
