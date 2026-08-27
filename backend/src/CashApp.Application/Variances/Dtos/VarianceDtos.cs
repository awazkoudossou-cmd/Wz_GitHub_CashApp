using CashApp.Domain.Enums;

namespace CashApp.Application.Variances.Dtos;

public record VarianceCaseListItemDto(
    int Id,
    int CashSessionId,
    int CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    decimal VarianceAmount,
    string CurrencyCode,
    VarianceStatus Status,
    DateTime DetectedAt,
    int? ApprovalRequestId,
    int? AnomalyCaseId,
    // Infos session reportées pour le menu Écarts de caisse.
    DateTime SessionOpenedAt,
    DateTime? SessionClosedAt,
    string SessionOpenedByName,
    string? SessionClosedByName,
    decimal SessionOpeningBalance,
    decimal? SessionTheoreticalBalance,
    decimal? SessionPhysicalBalance);

public record VarianceJustificationDto(int Id, int ProvidedBy, string ProvidedByName, DateTime ProvidedAt, string Comment);
public record VarianceActionDto(int Id, string ActionType, int PerformedBy, string PerformedByName, DateTime PerformedAt, string? Comment);

public record VarianceCaseDetailDto(
    int Id,
    int CashSessionId,
    int CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    decimal VarianceAmount,
    string CurrencyCode,
    VarianceStatus Status,
    DateTime DetectedAt,
    int? ApprovalRequestId,
    int? AnomalyCaseId,
    DateTime? ClosedAt,
    DateTime SessionOpenedAt,
    DateTime? SessionClosedAt,
    string SessionOpenedByName,
    string? SessionClosedByName,
    decimal SessionOpeningBalance,
    decimal? SessionTheoreticalBalance,
    decimal? SessionPhysicalBalance,
    string? SessionCloseComment,
    IReadOnlyList<VarianceJustificationDto> Justifications,
    IReadOnlyList<VarianceActionDto> Actions);

public record CreateVarianceJustificationDto(string Comment);

public record VarianceFilterDto(
    VarianceStatus? Status,
    int? CashRegisterId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
