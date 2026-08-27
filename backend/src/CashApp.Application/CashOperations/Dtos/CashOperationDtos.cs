using CashApp.Domain.Enums;

namespace CashApp.Application.CashOperations.Dtos;

public record CashOperationListItemDto(
    int Id,
    string OperationRef,
    int CashRegisterId,
    string CashRegisterCode,
    int CashSessionId,
    CashSessionStatus CashSessionStatus,
    DateTime OperationDate,
    OperationDirection Direction,
    int CategoryId,
    string CategoryLabel,
    decimal Amount,
    string CurrencyCode,
    PaymentMethod PaymentMethod,
    string Label,
    string? ThirdPartyName,
    bool IsDeleted,
    bool IsLocked,
    bool IsPendingApproval,
    bool IsPendingCancellation,
    bool HasWorkflowHistory);

public record CashOperationDetailDto(
    int Id,
    string OperationRef,
    int CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    int CashSessionId,
    CashSessionStatus CashSessionStatus,
    DateTime OperationDate,
    OperationDirection Direction,
    int CategoryId,
    string CategoryCode,
    string CategoryLabel,
    decimal Amount,
    string CurrencyCode,
    PaymentMethod PaymentMethod,
    string Label,
    string? Description,
    string? ExternalReference,
    string? ThirdPartyName,
    int? CreatedBy,
    DateTime CreatedAt,
    int? UpdatedBy,
    DateTime? UpdatedAt,
    bool IsDeleted,
    int? DeletedBy,
    DateTime? DeletedAt,
    string? DeleteReason,
    bool IsLocked,
    string? LockedByType,
    string? LockedByReference,
    int? LockedById,
    bool IsPendingApproval,
    bool IsPendingCancellation,
    bool HasWorkflowHistory);

public record CreateCashOperationDto(
    int CashSessionId,
    DateTime OperationDate,
    OperationDirection Direction,
    int CategoryId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string Label,
    string? Description,
    string? ExternalReference,
    string? ThirdPartyName);

public record UpdateCashOperationDto(
    DateTime OperationDate,
    int CategoryId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string Label,
    string? Description,
    string? ExternalReference,
    string? ThirdPartyName);

public record CancelCashOperationDto(string Reason);

public record CashOperationFilterDto(
    int? CashRegisterId,
    int? CashSessionId,
    DateTime? From,
    DateTime? To,
    OperationDirection? Direction,
    int? CategoryId,
    int Page = 1,
    int PageSize = 50,
    string? SortBy = null,      // "date" | "ref" (defaut: date)
    string? SortDir = null,     // "asc" | "desc" (defaut: desc)
    bool IncludeDeleted = false); // true = inclut les ops soft-deleted (annulées/rejetées) — n'affecte pas les calculs (filtre client/UI uniquement)
