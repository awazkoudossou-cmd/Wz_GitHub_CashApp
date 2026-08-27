using CashApp.Domain.Enums;

namespace CashApp.Application.Reports.Dtos;

// === Filtres ===

public record CashReportFilterDto(DateTime From, DateTime To, int? CashRegisterId);
public record CategoryReportFilterDto(DateTime From, DateTime To, int? CashRegisterId, OperationDirection? Direction);
public record VarianceReportFilterDto(DateTime From, DateTime To, int? CashRegisterId, VarianceStatus? Status);
public record TransferReportFilterDto(DateTime From, DateTime To, int? CashRegisterId, CashTransferStatus? Status);
public record DepositReportFilterDto(DateTime From, DateTime To, int? CashRegisterId, BankDepositStatus? Status);
public record AnomalyReportFilterDto(DateTime From, DateTime To, int? CashRegisterId, AnomalyStatus? Status, AnomalySeverity? Severity);
public record ApprovalReportFilterDto(DateTime From, DateTime To, ApprovalStatus? Status, ApprovalTargetType? TargetType);

// === Lignes ===

public record CashReportRowDto(int CashRegisterId, string CashRegisterCode, decimal TotalIn, decimal TotalOut, decimal NetMovement, int OperationCount);
public record CategoryReportRowDto(int CategoryId, string CategoryCode, string CategoryLabel, OperationDirection Direction, decimal Total, int Count);
public record VarianceReportRowDto(int VarianceCaseId, int CashSessionId, int CashRegisterId, string CashRegisterCode, decimal VarianceAmount, VarianceStatus Status, DateTime DetectedAt);
public record TransferReportRowDto(int Id, string TransferRef, string SourceCode, string DestinationCode, decimal Amount, string CurrencyCode, CashTransferStatus Status, DateTime TransferDate);
public record DepositReportRowDto(int Id, string DepositRef, string CashRegisterCode, string BankName, decimal Amount, string CurrencyCode, BankDepositStatus Status, DateTime DepositDate);
public record AnomalyReportRowDto(int Id, string Reference, AnomalySeverity Severity, AnomalyStatus Status, string? CashRegisterCode, DateTime DetectedAt);
public record ApprovalReportRowDto(int Id, string RequestRef, ApprovalTargetType TargetType, string TargetEntityType, ApprovalStatus Status, decimal? Amount, DateTime RequestedAt);

// === Synthèses ===
public record CashReportSummaryDto(decimal TotalIn, decimal TotalOut, decimal Net, int OperationCount);
public record CashReportResultDto(CashReportSummaryDto Summary, IReadOnlyList<CashReportRowDto> Rows);
public record CategoryReportResultDto(IReadOnlyList<CategoryReportRowDto> Rows);
public record VarianceReportResultDto(IReadOnlyList<VarianceReportRowDto> Rows);
public record TransferReportResultDto(IReadOnlyList<TransferReportRowDto> Rows);
public record DepositReportResultDto(IReadOnlyList<DepositReportRowDto> Rows);
public record AnomalyReportResultDto(IReadOnlyList<AnomalyReportRowDto> Rows);
public record ApprovalReportResultDto(IReadOnlyList<ApprovalReportRowDto> Rows);
