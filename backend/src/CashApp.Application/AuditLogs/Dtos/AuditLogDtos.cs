using CashApp.Domain.Enums;

namespace CashApp.Application.AuditLogs.Dtos;

public record AuditLogListItemDto(
    int Id,
    AuditAction ActionType,
    string EntityType,
    int? EntityId,
    int? PerformedBy,
    string? PerformedByName,
    DateTime PerformedAt,
    string? Description,
    decimal? Amount,
    string? CurrencyCode);

public record AuditLogDetailDto(
    int Id,
    AuditAction ActionType,
    string EntityType,
    int? EntityId,
    int? PerformedBy,
    string? PerformedByName,
    DateTime PerformedAt,
    string? Description,
    string? OldValuesJson,
    string? NewValuesJson,
    string? MetadataJson,
    string? IpAddress);

public record AuditLogFilterDto(
    AuditAction? ActionType,
    string? EntityType,
    int? EntityId,
    int? PerformedBy,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 100);
