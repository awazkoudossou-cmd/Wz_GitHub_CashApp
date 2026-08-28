using System.Text;
using CashApp.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using CashApp.Api.Services;
using CashApp.Application;
using CashApp.Application.Common.Interfaces;
using CashApp.Infrastructure;
using CashApp.Infrastructure.Persistence;
using CashApp.Infrastructure.Persistence.Seed;
using CashApp.Infrastructure.Security;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Port dynamique (Render/Heroku fournissent PORT ; fallback 5080 en local) ---
var port = Environment.GetEnvironmentVariable("PORT");
var isBehindProxy = !string.IsNullOrWhiteSpace(port);
if (isBehindProxy)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Render (et la plupart des PaaS) terminent le TLS sur leur proxy et relaient en HTTP :
// sans ça, UseHttpsRedirection() ci-dessous provoquerait une boucle de redirection infinie.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// --- Configuration ---
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Jwt section missing in configuration.");

// --- Core services ---
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Sérialise/désérialise les enums sous forme de chaînes (IN/OUT, ESSENTIAL, etc.)
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IHttpContextInfo, HttpContextInfo>();

// --- Layers ---
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// --- AuthN/AuthZ ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(2),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();

// --- CORS ---
// Origines par défaut (dev local) + origines supplémentaires via la variable d'env/config
// "Cors__AllowedOrigins__0", "Cors__AllowedOrigins__1", ... (ex: l'URL du frontend Vercel en prod).
const string DevCors = "DevCors";
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var corsOrigins = new[] { "http://localhost:5173", "http://localhost:3000" }
    .Concat(configuredOrigins)
    .Distinct()
    .ToArray();
builder.Services.AddCors(o => o.AddPolicy(DevCors, p =>
    p.WithOrigins(corsOrigins)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// --- Swagger + JWT in header ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CashApp API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT token. Format: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Id = JwtBearerDefaults.AuthenticationScheme, Type = ReferenceType.SecurityScheme }
    };
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });
});

var app = builder.Build();

// --- Migrate + seed on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await db.Database.EnsureCreatedAsync();

    // Migrations légères ad-hoc : ajout de colonnes V2-A si elles n'existent pas.
    // SQLite ne supporte pas "ADD COLUMN IF NOT EXISTS" → on tente et on ignore l'erreur.
    // Ce bloc est écrit en dialecte SQLite et n'a de sens que pour faire évoluer une base
    // SQLite locale déjà créée avant l'ajout de ces colonnes au modèle EF. Sur Postgres,
    // EnsureCreatedAsync ci-dessus crée déjà le schéma complet et à jour depuis le modèle EF :
    // ce bloc est donc sauté (sa syntaxe SQLite ferait de toute façon échouer chaque requête).
    if (db.Database.IsSqlite())
    {
    static async Task TryExec(AppDbContext db, string sql)
    {
        try { await db.Database.ExecuteSqlRawAsync(sql); } catch { /* déjà appliquée */ }
    }
    await TryExec(db, "ALTER TABLE cash_operations ADD COLUMN IsPendingApproval INTEGER NOT NULL DEFAULT 0;");
    await TryExec(db, "ALTER TABLE cash_operations ADD COLUMN IsPendingCancellation INTEGER NOT NULL DEFAULT 0;");
    await TryExec(db, "ALTER TABLE cash_registers ADD COLUMN DefaultDirection TEXT NOT NULL DEFAULT 'IN';");
    await TryExec(db, "ALTER TABLE cash_registers ADD COLUMN DefaultPaymentMethod TEXT NOT NULL DEFAULT 'CASH';");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS third_parties (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL COLLATE NOCASE,
        IsActive INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, "CREATE UNIQUE INDEX IF NOT EXISTS IX_third_parties_Name ON third_parties(Name);");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS category_groups (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL COLLATE NOCASE,
        IsActive INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, "CREATE UNIQUE INDEX IF NOT EXISTS IX_category_groups_Name ON category_groups(Name);");
    await TryExec(db, "ALTER TABLE categories ADD COLUMN GroupId INTEGER NULL;");

    // V3 — Comptabilité : nouvelles tables + colonnes de rattachement sur les tables V1 existantes.
    await TryExec(db, "ALTER TABLE cash_registers ADD COLUMN AccountingJournalId INTEGER NULL;");
    await TryExec(db, "ALTER TABLE cash_registers ADD COLUMN AccountingAccountId INTEGER NULL;");
    await TryExec(db, "ALTER TABLE categories ADD COLUMN AccountingAccountId INTEGER NULL;");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS accounting_journals (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Code TEXT NOT NULL,
        Name TEXT NOT NULL,
        Description TEXT NULL,
        IsActive INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, "CREATE UNIQUE INDEX IF NOT EXISTS IX_accounting_journals_Code ON accounting_journals(Code);");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS accounting_accounts (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        AccountNumber TEXT NOT NULL,
        Name TEXT NOT NULL,
        Nature TEXT NOT NULL,
        IsActive INTEGER NOT NULL DEFAULT 1,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, "CREATE UNIQUE INDEX IF NOT EXISTS IX_accounting_accounts_AccountNumber ON accounting_accounts(AccountNumber);");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS accounting_settings (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        GenerationType TEXT NOT NULL,
        GenerationMode TEXT NOT NULL,
        NarrationTemplate TEXT NULL,
        IsConfigured INTEGER NOT NULL DEFAULT 0,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS accounting_generations (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Reference TEXT NOT NULL,
        GenerationType TEXT NOT NULL,
        GenerationMode TEXT NOT NULL,
        StartDate TEXT NOT NULL,
        EndDate TEXT NOT NULL,
        Status TEXT NOT NULL,
        GeneratedBy INTEGER NOT NULL,
        GeneratedAt TEXT NOT NULL,
        Exported INTEGER NOT NULL DEFAULT 0,
        ExportedAt TEXT NULL,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, "CREATE UNIQUE INDEX IF NOT EXISTS IX_accounting_generations_Reference ON accounting_generations(Reference);");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS accounting_entries (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        GenerationId INTEGER NOT NULL,
        CashOperationId INTEGER NULL,
        JournalId INTEGER NOT NULL,
        AccountId INTEGER NOT NULL,
        EntryDate TEXT NOT NULL,
        OperationDate TEXT NOT NULL,
        Reference TEXT NOT NULL,
        PieceNumber TEXT NULL,
        Description TEXT NOT NULL,
        Debit TEXT NOT NULL DEFAULT 0,
        Credit TEXT NOT NULL DEFAULT 0,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS accounting_pendings (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CashOperationId INTEGER NOT NULL,
        Reason TEXT NOT NULL,
        CreatedDate TEXT NOT NULL,
        Resolved INTEGER NOT NULL DEFAULT 0,
        ResolvedDate TEXT NULL,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, "ALTER TABLE accounting_generations ADD COLUMN TotalOperations INTEGER NOT NULL DEFAULT 0;");
    await TryExec(db, "ALTER TABLE accounting_generations ADD COLUMN TotalEntries INTEGER NOT NULL DEFAULT 0;");
    await TryExec(db, "ALTER TABLE accounting_generations ADD COLUMN Remarks TEXT NULL;");
    await TryExec(db, "ALTER TABLE accounting_entries ADD COLUMN Comment TEXT NULL;");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS accounting_generation_logs (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        GenerationId INTEGER NOT NULL,
        PerformedBy INTEGER NOT NULL,
        PerformedAt TEXT NOT NULL,
        OperationCount INTEGER NOT NULL DEFAULT 0,
        EntryCount INTEGER NOT NULL DEFAULT 0,
        ProcessingTimeMs INTEGER NOT NULL DEFAULT 0,
        ErrorsJson TEXT NULL,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS accounting_generation_queues (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        CreatedDate TEXT NOT NULL,
        RequestedBy INTEGER NOT NULL,
        GenerationMode TEXT NOT NULL,
        StartDate TEXT NOT NULL,
        EndDate TEXT NOT NULL,
        Status TEXT NOT NULL,
        Priority INTEGER NOT NULL DEFAULT 0,
        CashRegisterIdsJson TEXT NULL,
        Remarks TEXT NULL,
        RetryCount INTEGER NOT NULL DEFAULT 0,
        StartedDate TEXT NULL,
        CompletedDate TEXT NULL,
        ResultGenerationId INTEGER NULL,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");
    await TryExec(db, @"CREATE TABLE IF NOT EXISTS accounting_export_logs (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ExportType TEXT NOT NULL,
        GenerationId INTEGER NULL,
        FileName TEXT NOT NULL,
        ContentType TEXT NOT NULL,
        FileContent BLOB NOT NULL,
        ExportedBy INTEGER NOT NULL,
        ExportedAt TEXT NOT NULL,
        LineCount INTEGER NOT NULL DEFAULT 0,
        CreatedAt TEXT NOT NULL,
        UpdatedAt TEXT NULL
    );");

    // V3_7 — Centre d'Exports Comptables : bascule du stockage BLOB vers un fichier sur disque (FilePath).
    await TryExec(db, "ALTER TABLE accounting_export_logs ADD COLUMN ExportNumber TEXT NOT NULL DEFAULT '';");
    await TryExec(db, "ALTER TABLE accounting_export_logs ADD COLUMN GenerationType TEXT NULL;");
    await TryExec(db, "ALTER TABLE accounting_export_logs ADD COLUMN GenerationMode TEXT NULL;");
    await TryExec(db, "ALTER TABLE accounting_export_logs ADD COLUMN FilePath TEXT NOT NULL DEFAULT '';");
    await TryExec(db, "ALTER TABLE accounting_export_logs ADD COLUMN Status TEXT NOT NULL DEFAULT 'GENERATED';");
    await TryExec(db, "ALTER TABLE accounting_export_logs ADD COLUMN FilterJson TEXT NULL;");
    await TryExec(db, "ALTER TABLE accounting_export_logs ADD COLUMN ProcessingTimeMs INTEGER NOT NULL DEFAULT 0;");
    await TryExec(db, "ALTER TABLE accounting_export_logs ADD COLUMN Remarks TEXT NULL;");
    await TryExec(db, "ALTER TABLE accounting_export_logs ADD COLUMN DownloadedAt TEXT NULL;");
    await TryExec(db, "ALTER TABLE accounting_export_logs DROP COLUMN FileContent;");
    await TryExec(db, "CREATE UNIQUE INDEX IF NOT EXISTS IX_accounting_export_logs_ExportNumber ON accounting_export_logs(ExportNumber);");

    // V3_9 — Numérotation automatique compte/journal des caisses.
    await TryExec(db, "ALTER TABLE accounting_settings ADD COLUMN CashAccountRootNumber TEXT NULL;");
    await TryExec(db, "ALTER TABLE accounting_settings ADD COLUMN CashAccountNumberLength INTEGER NULL;");
    await TryExec(db, "ALTER TABLE accounting_settings ADD COLUMN CashJournalRootCode TEXT NULL;");
    await TryExec(db, "ALTER TABLE accounting_settings ADD COLUMN LastCashAccountSequence INTEGER NOT NULL DEFAULT 0;");
    await TryExec(db, "ALTER TABLE accounting_settings ADD COLUMN LastCashJournalSequence INTEGER NOT NULL DEFAULT 0;");
    }

    await DbSeeder.SeedAsync(db, hasher);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseForwardedHeaders();
if (!isBehindProxy)
{
    app.UseHttpsRedirection();
}
app.UseCors(DevCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposé pour les tests d'intégration.
public partial class Program { }
