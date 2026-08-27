using CashApp.Domain.Enums;

namespace CashApp.Application.Imports.Dtos;

public record ImportBatchListItemDto(
    int Id,
    string BatchRef,
    ImportBatchType BatchType,
    string OriginalFileName,
    int UploadedBy,
    string UploadedByName,
    DateTime UploadedAt,
    ImportBatchStatus Status,
    int TotalLines,
    int ValidLines,
    int InvalidLines,
    int ImportedLines,
    int? CashRegisterId);

public record ImportBatchDetailDto(
    int Id,
    string BatchRef,
    ImportBatchType BatchType,
    string OriginalFileName,
    int UploadedBy,
    string UploadedByName,
    DateTime UploadedAt,
    ImportBatchStatus Status,
    int TotalLines,
    int ValidLines,
    int InvalidLines,
    int ImportedLines,
    int? CashRegisterId,
    string? CashRegisterCode,
    string? ErrorSummaryJson,
    IReadOnlyList<ImportPreviewLineDto> Lines);

public record ImportPreviewLineDto(
    int LineNumber,
    ImportLineStatus Status,
    string? ErrorMessage,
    string RawDataJson,
    string? ParsedDataJson,
    int? CreatedEntityId);

public record ImportPreviewDto(
    int BatchId,
    int TotalLines,
    int ValidLines,
    int InvalidLines,
    IReadOnlyList<ImportPreviewLineDto> Lines);

public record ConfirmImportDto(bool AllowPartialSuccess);
