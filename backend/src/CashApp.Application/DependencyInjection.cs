using CashApp.Application.Access;
using CashApp.Application.Accounting;
using CashApp.Application.Anomalies;
using CashApp.Application.Approvals;
using CashApp.Application.Attachments;
using CashApp.Application.Audit;
using CashApp.Application.AuditLogs;
using CashApp.Application.Auth;
using CashApp.Application.BankDeposits;
using CashApp.Application.CashOperations;
using CashApp.Application.CashRegisters;
using CashApp.Application.CashSessions;
using CashApp.Application.CashTransfers;
using CashApp.Application.Categories;
using CashApp.Application.CategoryGroups;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Dashboard;
using CashApp.Application.Features;
using CashApp.Application.Imports;
using CashApp.Application.Imports.Parsers;
using CashApp.Application.Reconciliation;
using CashApp.Application.References;
using CashApp.Application.Reports;
using CashApp.Application.Settings;
using CashApp.Application.ThirdParties;
using CashApp.Application.Users;
using CashApp.Application.Variances;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CashApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // V1
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICashRegisterService, CashRegisterService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICategoryGroupService, CategoryGroupService>();
        services.AddScoped<IThirdPartyService, ThirdPartyService>();
        services.AddScoped<ICashSessionService, CashSessionService>();
        services.AddScoped<ICashOperationService, CashOperationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IFeatureService, FeatureService>();
        services.AddScoped<IReferenceGeneratorService, ReferenceGeneratorService>();

        // V2-A — transverses
        services.AddScoped<ICashRegisterAccessService, CashRegisterAccessService>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        // V2-A — modules
        services.AddScoped<IApprovalRuleService, ApprovalRuleService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<ICashTransferService, CashTransferService>();
        services.AddScoped<IBankDepositService, BankDepositService>();
        services.AddScoped<IAnomalyService, AnomalyService>();
        services.AddScoped<IVarianceService, VarianceService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // V2-B
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IImportParser, CashOperationImportParser>();
        services.AddScoped<IImportParser, CategoryImportParser>();
        services.AddScoped<IImportParser, AccountingAccountImportParser>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IAdvancedReportService, AdvancedReportService>();

        // V3 — Comptabilité
        services.AddScoped<IAccountingJournalService, AccountingJournalService>();
        services.AddScoped<IAccountingAccountService, AccountingAccountService>();
        services.AddScoped<IAccountingSettingsService, AccountingSettingsService>();
        services.AddScoped<IAccountingGenerationService, AccountingGenerationService>();
        services.AddScoped<IAccountingWizardService, AccountingWizardService>();
        services.AddScoped<IAccountingGenerationEngineService, AccountingGenerationEngineService>();
        services.AddScoped<IAccountingEntryService, AccountingEntryService>();
        services.AddScoped<IAccountingQueueService, AccountingQueueService>();
        services.AddScoped<IAccountingRetryService, AccountingRetryService>();
        services.AddScoped<IAccountingDashboardService, AccountingDashboardService>();
        services.AddScoped<IAccountingCashRegisterProvisioningService, AccountingCashRegisterProvisioningService>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
