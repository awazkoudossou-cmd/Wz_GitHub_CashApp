using CashApp.Domain.Enums;

namespace CashApp.Application.Approvals.Dtos;

// === ApprovalRule ===
public record ApprovalRuleDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    ApprovalTargetType TargetType,
    decimal? AmountThreshold,
    string? CurrencyCode,
    string RequiredApproverRole,
    bool IsBlocking,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateApprovalRuleDto(
    string Code,
    string Name,
    string? Description,
    ApprovalTargetType TargetType,
    decimal? AmountThreshold,
    string? CurrencyCode,
    string RequiredApproverRole,
    bool IsBlocking);

public record UpdateApprovalRuleDto(
    string Name,
    string? Description,
    decimal? AmountThreshold,
    string? CurrencyCode,
    string RequiredApproverRole,
    bool IsBlocking);

public record UpdateApprovalRuleStatusDto(bool IsActive);

// === ApprovalRequest ===
public record ApprovalRequestListItemDto(
    int Id,
    string RequestRef,
    ApprovalTargetType TargetType,
    string TargetEntityType,
    int TargetEntityId,
    int? CashRegisterId,
    string? CashRegisterCode,
    decimal? Amount,
    string? CurrencyCode,
    ApprovalStatus Status,
    int RequestedBy,
    string RequestedByName,
    DateTime RequestedAt,
    int? DecidedBy,
    string? DecidedByName,
    DateTime? DecidedAt,
    string Reason,
    int? CashSessionId);   // résolu via la cible (op/transfert/dépôt/session)

public record ApprovalRequestDetailDto(
    int Id,
    string RequestRef,
    int ApprovalRuleId,
    string ApprovalRuleCode,
    ApprovalTargetType TargetType,
    string TargetEntityType,
    int TargetEntityId,
    int? CashRegisterId,
    string? CashRegisterCode,
    decimal? Amount,
    string? CurrencyCode,
    string Reason,
    ApprovalStatus Status,
    int RequestedBy,
    string RequestedByName,
    DateTime RequestedAt,
    int? DecidedBy,
    string? DecidedByName,
    DateTime? DecidedAt,
    string? DecisionComment,
    DateTime CreatedAt,
    IReadOnlyList<ApprovalActionDto> Actions);

public record ApprovalActionDto(
    int Id,
    AuditAction ActionType,
    int PerformedBy,
    string PerformedByName,
    DateTime PerformedAt,
    string? Comment);

public record ApproveRequestDto(string? Comment);
public record RejectRequestDto(string Comment);

public record ApprovalRequestFilterDto(
    ApprovalStatus? Status,
    ApprovalTargetType? TargetType,
    int? CashRegisterId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50,
    string? SortBy = null,   // "date" | "amount" (defaut: date)
    string? SortDir = null); // "asc" | "desc" (defaut: desc)
