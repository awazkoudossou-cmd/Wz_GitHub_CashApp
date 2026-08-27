using CashApp.Application.Reports.Dtos;

namespace CashApp.Application.Reports;

public interface IAdvancedReportService
{
    Task<CashReportResultDto> CashAsync(CashReportFilterDto filter, CancellationToken ct = default);
    Task<CategoryReportResultDto> CategoriesAsync(CategoryReportFilterDto filter, CancellationToken ct = default);
    Task<VarianceReportResultDto> VariancesAsync(VarianceReportFilterDto filter, CancellationToken ct = default);
    Task<TransferReportResultDto> TransfersAsync(TransferReportFilterDto filter, CancellationToken ct = default);
    Task<DepositReportResultDto> DepositsAsync(DepositReportFilterDto filter, CancellationToken ct = default);
    Task<AnomalyReportResultDto> AnomaliesAsync(AnomalyReportFilterDto filter, CancellationToken ct = default);
    Task<ApprovalReportResultDto> ApprovalsAsync(ApprovalReportFilterDto filter, CancellationToken ct = default);
}
