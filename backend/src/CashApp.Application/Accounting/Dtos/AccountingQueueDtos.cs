using CashApp.Domain.Enums;

namespace CashApp.Application.Accounting.Dtos;

public record EnqueueManualGenerationDto(
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<int>? CashRegisterIds,
    int Priority = 0);

public record AccountingQueueItemDto(
    int Id,
    DateTime CreatedDate,
    int RequestedBy,
    string RequestedByName,
    AccountingGenerationMode GenerationMode,
    DateTime StartDate,
    DateTime EndDate,
    QueueStatus Status,
    int Priority,
    IReadOnlyList<int>? CashRegisterIds,
    string? Remarks,
    int RetryCount,
    DateTime? StartedDate,
    DateTime? CompletedDate,
    int? ResultGenerationId,
    string? ResultGenerationReference);

public record AccountingQueueFilterDto(
    QueueStatus? Status,
    int Page = 1,
    int PageSize = 50);
