using CashApp.Domain.Enums;

namespace CashApp.Application.Accounting.Dtos;

public record AccountingExportLogDto(
    int Id,
    string ExportNumber,
    string ExportType,
    int? GenerationId,
    string? GenerationReference,
    AccountingGenerationType? GenerationType,
    AccountingGenerationMode? GenerationMode,
    string FileName,
    int ExportedBy,
    string ExportedByName,
    DateTime ExportedAt,
    int LineCount,
    AccountingExportStatus Status);

public record AccountingExportLogFilterDto(
    int Page = 1,
    int PageSize = 50,
    AccountingExportStatus? Status = null,
    string? ExportType = null);

public record AccountingExportPreviewDto(
    int EntryCount,
    int BatchCount,
    int AccountCount,
    int JournalCount,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    decimal TotalDebit,
    decimal TotalCredit,
    bool IsBalanced,
    long EstimatedSizeBytes);

public record AccountingExportDetailDto(
    int Id,
    string ExportNumber,
    string ExportType,
    int? GenerationId,
    string? GenerationReference,
    string? FilterDescription,
    int ExportedBy,
    string ExportedByName,
    DateTime ExportedAt,
    int LineCount,
    int ProcessingTimeMs,
    AccountingExportStatus Status,
    DateTime? DownloadedAt,
    string? Remarks);
