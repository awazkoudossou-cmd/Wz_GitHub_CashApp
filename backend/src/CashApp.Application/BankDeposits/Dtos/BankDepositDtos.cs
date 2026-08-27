using CashApp.Domain.Enums;

namespace CashApp.Application.BankDeposits.Dtos;

public record BankDepositListItemDto(
    int Id,
    string DepositRef,
    int CashRegisterId,
    string CashRegisterCode,
    DateTime DepositDate,
    decimal Amount,
    string CurrencyCode,
    string BankName,
    BankDepositStatus Status,
    int RequestedBy,
    string RequestedByName,
    DateTime CreatedAt);

public record BankDepositDetailDto(
    int Id,
    string DepositRef,
    int CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    int? CashSessionId,
    DateTime DepositDate,
    decimal Amount,
    string CurrencyCode,
    string BankName,
    string? AccountReference,
    string? DepositSlipReference,
    string? Description,
    BankDepositStatus Status,
    int RequestedBy,
    string RequestedByName,
    int? ApprovedBy,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    int? LinkedOperationId,
    string? LinkedOperationRef,
    int? ApprovalRequestId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateBankDepositDto(
    int CashRegisterId,
    DateTime DepositDate,
    decimal Amount,
    string CurrencyCode,
    string BankName,
    string? AccountReference,
    string? DepositSlipReference,
    string? Description);

public record ApproveBankDepositDto(string? Comment);
public record RejectBankDepositDto(string Comment);
public record CancelBankDepositDto(string Reason);

public record BankDepositFilterDto(
    BankDepositStatus? Status,
    int? CashRegisterId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
