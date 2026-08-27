using CashApp.Domain.Enums;

namespace CashApp.Application.CashTransfers.Dtos;

public record CashTransferListItemDto(
    int Id,
    string TransferRef,
    int SourceCashRegisterId,
    string SourceCashRegisterCode,
    int DestinationCashRegisterId,
    string DestinationCashRegisterCode,
    decimal Amount,
    string CurrencyCode,
    DateTime TransferDate,
    CashTransferStatus Status,
    int RequestedBy,
    string RequestedByName,
    DateTime CreatedAt);

public record CashTransferDetailDto(
    int Id,
    string TransferRef,
    int SourceCashRegisterId,
    string SourceCashRegisterCode,
    string SourceCashRegisterName,
    int? SourceSessionId,
    int DestinationCashRegisterId,
    string DestinationCashRegisterCode,
    string DestinationCashRegisterName,
    int? DestinationSessionId,
    decimal Amount,
    string CurrencyCode,
    DateTime TransferDate,
    string Reason,
    CashTransferStatus Status,
    int RequestedBy,
    string RequestedByName,
    int? ApprovedBy,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    int? SourceOperationId,
    string? SourceOperationRef,
    int? DestinationOperationId,
    string? DestinationOperationRef,
    int? ApprovalRequestId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateCashTransferDto(
    int SourceCashRegisterId,
    int DestinationCashRegisterId,
    decimal Amount,
    string CurrencyCode,
    DateTime TransferDate,
    string Reason);

public record ApproveCashTransferDto(string? Comment);
public record RejectCashTransferDto(string Comment);
public record CancelCashTransferDto(string Reason);

public record CashTransferFilterDto(
    CashTransferStatus? Status,
    int? SourceCashRegisterId,
    int? DestinationCashRegisterId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
