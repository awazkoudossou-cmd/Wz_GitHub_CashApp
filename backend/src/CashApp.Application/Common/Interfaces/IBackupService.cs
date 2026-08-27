namespace CashApp.Application.Common.Interfaces;

public interface IBackupService
{
    Task<string> CreateBackupAsync(CancellationToken ct = default);
    Task RestoreBackupAsync(string backupFilePath, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListBackupsAsync(CancellationToken ct = default);
}
