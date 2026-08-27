using CashApp.Application.Accounting;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Domain.Constants;
using CashApp.Infrastructure.Persistence;
using CashApp.Infrastructure.Services;
using NSubstitute;

namespace CashApp.Application.Tests.Fakes;

// Construit un AccountingExportService prêt pour les tests, avec un ISettingsService factice
// qui pointe le stockage des exports vers un dossier temporaire (nettoyé par l'OS).
public static class AccountingExportServiceTestFactory
{
    public static AccountingExportService Create(AppDbContext db, IDateTimeProvider clock, IAccountingEntryService entries,
        ICurrentUserService? user = null, IAuditLogger? audit = null)
    {
        var settings = Substitute.For<ISettingsService>();
        var root = Path.Combine(Path.GetTempPath(), "cashapp-tests-accounting-exports", Guid.NewGuid().ToString("N"));
        settings.GetRawAsync(SettingKeys.AccountingExportsRootPath, Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(root));

        var excel = new ExcelExportService();
        var auditLogger = audit ?? Substitute.For<IAuditLogger>();
        var download = new AccountingDownloadService(db, clock, auditLogger);

        return new AccountingExportService(db, clock, user ?? new FakeCurrentUser(), auditLogger, entries, settings, excel, download);
    }
}
