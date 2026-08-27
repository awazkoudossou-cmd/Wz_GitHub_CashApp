namespace CashApp.Application.Accounting.Dtos;

public record GenerateAccountingEntriesDto(
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<int>? CashRegisterIds);

public record AccountingGenerationPreviewDto(
    int OperationCount,
    int EstimatedEntryCount,
    int PendingCount,
    int EstimatedProcessingMs,
    int AlreadyAccountedCount,
    int IgnoredCount);
