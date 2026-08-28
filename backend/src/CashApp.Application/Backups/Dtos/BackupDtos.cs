namespace CashApp.Application.Backups.Dtos;

public record BackupListItemDto(
    int Id,
    string FileName,
    string FilePath,
    int? CreatedBy,
    string? CreatedByName,
    DateTime CreatedAt,
    long? SizeBytes);

public record CreateBackupResponseDto(
    int Id,
    string FileName,
    string FilePath,
    DateTime CreatedAt);

public record RestoreBackupDto(string FileName);
