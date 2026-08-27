using CashApp.Domain.Enums;

namespace CashApp.Application.CashSessions.Dtos;

public record CashSessionListItemDto(
    int Id,
    int CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    int OpenedBy,
    string OpenedByName,
    DateTime OpenedAt,
    decimal OpeningBalance,
    int? ClosedBy,
    DateTime? ClosedAt,
    CashSessionStatus Status,
    decimal? TheoreticalBalance,
    decimal? PhysicalBalance,
    decimal? VarianceAmount);

public record CashSessionDetailDto(
    int Id,
    int CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    int OpenedBy,
    string OpenedByName,
    DateTime OpenedAt,
    decimal OpeningBalance,
    int? ClosedBy,
    string? ClosedByName,
    DateTime? ClosedAt,
    decimal? TheoreticalBalance,
    decimal? PhysicalBalance,
    decimal? VarianceAmount,
    CashSessionStatus Status,
    string? OpenComment,
    string? CloseComment,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CashSessionSummaryDto Summary);

public record OpenCashSessionDto(
    int CashRegisterId,
    decimal OpeningBalance,
    string? OpenComment);

public record CloseCashSessionDto(
    decimal PhysicalBalance,
    string? CloseComment,
    string? VarianceJustification = null,
    bool ConfirmDeletePending = false);

public record SessionPendingItemsDto(
    int PendingOperationCount,
    int PendingTransferCount,
    int PendingDepositCount,
    IReadOnlyList<string> PendingOperationRefs,
    IReadOnlyList<string> PendingTransferRefs,
    IReadOnlyList<string> PendingDepositRefs)
{
    public bool HasAny => PendingOperationCount + PendingTransferCount + PendingDepositCount > 0;
}

public record CashSessionSummaryDto(
    int OperationCount,
    decimal TotalIn,
    decimal TotalOut,
    decimal NetMovement);

public record OpeningDefaultDto(
    int CashRegisterId,
    decimal DefaultOpeningBalance,
    string Source);   // "ZERO" | "LAST_CLOSING_PHYSICAL" | "NO_PREVIOUS_SESSION"
