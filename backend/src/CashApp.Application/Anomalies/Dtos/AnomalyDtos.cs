using CashApp.Domain.Enums;

namespace CashApp.Application.Anomalies.Dtos;

public record AnomalyListItemDto(
    int Id,
    string Reference,
    AnomalySeverity Severity,
    AnomalyStatus Status,
    string Title,
    int? CashRegisterId,
    string? CashRegisterCode,
    DateTime DetectedAt,
    int? AssignedTo,
    string? AssignedToName);

public record AnomalyDetailDto(
    int Id,
    string Reference,
    AnomalySeverity Severity,
    AnomalyStatus Status,
    string Title,
    string? Description,
    string? RelatedEntityType,
    int? RelatedEntityId,
    int? CashRegisterId,
    string? CashRegisterCode,
    int? CashSessionId,
    DateTime DetectedAt,
    int? DetectedBy,
    string? DetectedByName,
    int? AssignedTo,
    string? AssignedToName,
    DateTime? AssignedAt,
    DateTime? ResolvedAt,
    int? ResolvedBy,
    string? ResolvedByName,
    string? ResolutionComment,
    IReadOnlyList<AnomalyCommentDto> Comments);

public record AnomalyCommentDto(int Id, int AuthorId, string AuthorName, string Body, DateTime CreatedAt);

public record CreateAnomalyDto(
    AnomalySeverity Severity,
    string Title,
    string? Description,
    string? RelatedEntityType,
    int? RelatedEntityId,
    int? CashRegisterId,
    int? CashSessionId);

public record AssignAnomalyDto(int AssignToUserId);
public record ResolveAnomalyDto(string ResolutionComment);
public record AddAnomalyCommentDto(string Body);

public record AnomalyFilterDto(
    AnomalyStatus? Status,
    AnomalySeverity? Severity,
    int? CashRegisterId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
