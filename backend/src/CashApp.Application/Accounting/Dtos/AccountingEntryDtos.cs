using CashApp.Domain.Enums;

namespace CashApp.Application.Accounting.Dtos;

public record AccountingEntryListItemDto(
    int Id,
    int GenerationId,
    string BatchReference,
    DateTime GenerationDate,
    AccountingGenerationStatus BatchStatus,
    int? CashOperationId,
    string? CashOperationRef,
    DateTime OperationDate,
    DateTime EntryDate,
    int JournalId,
    string JournalCode,
    string Reference,
    string? PieceNumber,
    int AccountId,
    string AccountNumber,
    string AccountName,
    string Description,
    string? Comment,
    decimal Debit,
    decimal Credit,
    int? UserId,
    string UserName,
    bool IsLocked);

public record AccountingEntryDetailDto(
    int Id,
    int GenerationId,
    string BatchReference,
    DateTime GenerationDate,
    AccountingGenerationStatus BatchStatus,
    int? CashOperationId,
    string? CashOperationRef,
    int? CashSessionId,
    int? CashRegisterId,
    string? CashRegisterCode,
    int? CategoryId,
    string? CategoryCode,
    string? CategoryLabel,
    int? UserId,
    string UserName,
    int JournalId,
    string JournalCode,
    string JournalName,
    int AccountId,
    string AccountNumber,
    string AccountName,
    DateTime OperationDate,
    DateTime EntryDate,
    string Reference,
    string? PieceNumber,
    string Description,
    string? Comment,
    decimal Debit,
    decimal Credit,
    bool IsLocked);

public record UpdateAccountingEntryDto(string? Description, string? Comment);

public record AccountingEntryFilterDto(
    DateTime? From,
    DateTime? To,
    int? JournalId,
    int? AccountId,
    int? CashRegisterId,
    int? CategoryId,
    int? UserId,
    int? GenerationId,
    bool? Locked,
    string? Reference,
    string? PieceNumber,
    string? Search,
    string? SortBy,
    bool SortDescending = false,
    int Page = 1,
    int PageSize = 50,
    AccountingGenerationType? GenerationType = null,
    AccountingGenerationMode? GenerationMode = null);

public record AccountingLedgerStatsDto(
    int EntryCount,
    int BatchCount,
    int ExportedBatchCount,
    int PendingCount);
