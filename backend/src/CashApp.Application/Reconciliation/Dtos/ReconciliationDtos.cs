using CashApp.Domain.Enums;

namespace CashApp.Application.Reconciliation.Dtos;

public record ReconciliationBatchListItemDto(
    int Id,
    string Reference,
    ReconciliationBatchType BatchType,
    int? CashRegisterId,
    string? CashRegisterCode,
    int CreatedBy,
    string CreatedByName,
    ReconciliationStatus Status,
    DateTime CreatedAt);

public record ReconciliationItemDto(
    int Id,
    string LeftEntityType,
    int LeftEntityId,
    string? RightEntityType,
    int? RightEntityId,
    decimal? MatchedAmount,
    ReconciliationMatchStatus MatchStatus,
    string? Notes);

public record ReconciliationBatchDetailDto(
    int Id,
    string Reference,
    ReconciliationBatchType BatchType,
    int? CashRegisterId,
    string? CashRegisterCode,
    int CreatedBy,
    string CreatedByName,
    ReconciliationStatus Status,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<ReconciliationItemDto> Items);

public record CreateReconciliationBatchDto(
    ReconciliationBatchType BatchType,
    int? CashRegisterId,
    string? Notes);

public record ReconcileItemsDto(
    IReadOnlyList<ReconcilePairDto> Pairs,
    bool CloseAfter);

public record ReconcilePairDto(
    string LeftEntityType,
    int LeftEntityId,
    string? RightEntityType,
    int? RightEntityId,
    decimal? MatchedAmount,
    string? Notes);
