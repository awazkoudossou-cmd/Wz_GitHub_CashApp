using CashApp.Application.Accounting;
using CashApp.Application.Common.Interfaces;
using CashApp.Infrastructure.Persistence;
using CashApp.Infrastructure.Security;
using CashApp.Infrastructure.Services;
using CashApp.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CashApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=cashapp.db";

        services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<CashApp.Application.Admin.IDatabaseAdminService, DatabaseAdminService>();
        services.AddScoped<IAccountingExportService, AccountingExportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IAccountingDownloadService, AccountingDownloadService>();
        services.AddHostedService<AccountingWorker>();

        return services;
    }
}
