using CashApp.Application.Accounting;
using CashApp.Application.Common.Interfaces;
using CashApp.Infrastructure.Persistence;
using CashApp.Infrastructure.Security;
using CashApp.Infrastructure.Services;
using CashApp.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CashApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Data Source=cashapp.db";

        // Hébergement prod (Render + Vercel Postgres/Neon) : chaîne "postgres://..." ou "Host=...".
        // Dev local : "Data Source=..." (SQLite). Le provider EF est choisi en fonction du format reçu.
        var isPostgres = connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase);

        if (isPostgres)
        {
            var npgsqlConnectionString = NormalizePostgresConnectionString(connectionString);
            services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(npgsqlConnectionString));
        }
        else
        {
            services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(connectionString));
        }

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

    // Les fournisseurs Postgres managés (Vercel Postgres/Neon, Render, Heroku...) exposent une URI
    // "postgres://user:pass@host:port/db?sslmode=require" que Npgsql ne comprend pas nativement
    // (il attend un format clé=valeur). On la convertit ici.
    private static string NormalizePostgresConnectionString(string value)
    {
        if (!value.Contains("://", StringComparison.Ordinal))
            return value;

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
