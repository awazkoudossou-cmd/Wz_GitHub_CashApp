using CashApp.Domain.Enums;

namespace CashApp.Application.Accounting.Dtos;

public record AccountingGenerationListItemDto(
    int Id,
    string Reference,
    AccountingGenerationType GenerationType,
    AccountingGenerationMode GenerationMode,
    DateTime StartDate,
    DateTime EndDate,
    AccountingGenerationStatus Status,
    int GeneratedBy,
    string GeneratedByName,
    DateTime GeneratedAt,
    int TotalOperations,
    int EntryCount,
    bool Exported);

public record AccountingGenerationDetailDto(
    int Id,
    string Reference,
    AccountingGenerationType GenerationType,
    AccountingGenerationMode GenerationMode,
    DateTime StartDate,
    DateTime EndDate,
    AccountingGenerationStatus Status,
    int GeneratedBy,
    string GeneratedByName,
    DateTime GeneratedAt,
    bool Exported,
    DateTime? ExportedAt,
    int TotalOperations,
    int TotalEntries,
    decimal TotalDebit,
    decimal TotalCredit,
    int? ProcessingTimeMs,
    string? Remarks,
    IReadOnlyList<AccountingEntryDto> Entries);

public record AccountingEntryDto(
    int Id,
    int GenerationId,
    int? CashOperationId,
    string? CashOperationRef,
    int JournalId,
    string JournalCode,
    int AccountId,
    string AccountNumber,
    string AccountName,
    DateTime EntryDate,
    DateTime OperationDate,
    string Reference,
    string? PieceNumber,
    string Description,
    string? Comment,
    decimal Debit,
    decimal Credit);

public record AccountingGenerationFilterDto(
    AccountingGenerationStatus? Status,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);

public record AccountingPendingDto(
    int Id,
    int CashOperationId,
    string CashOperationRef,
    DateTime OperationDate,
    string Reason,
    DateTime CreatedDate,
    bool Resolved,
    DateTime? ResolvedDate);

public record AccountingPendingFilterDto(bool? Resolved, int Page = 1, int PageSize = 50);
