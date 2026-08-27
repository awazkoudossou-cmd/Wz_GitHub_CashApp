namespace CashApp.Application.Common.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportOperationsExcelAsync(DateTime from, DateTime to, int? cashRegisterId,
        string? direction, bool includeDeleted, CancellationToken ct = default);
    Task<byte[]> ExportOperationsPdfAsync(DateTime from, DateTime to, int? cashRegisterId, CancellationToken ct = default);
    Task<byte[]> ExportSessionsExcelAsync(DateTime from, DateTime to, int? cashRegisterId, CancellationToken ct = default);
    Task<byte[]> ExportSessionsPdfAsync(DateTime from, DateTime to, int? cashRegisterId, CancellationToken ct = default);
    Task<byte[]> ExportCashStatePdfAsync(int cashSessionId, CancellationToken ct = default);
    Task<byte[]> ExportOperationReceiptPdfAsync(int operationId, CancellationToken ct = default);
    Task<byte[]> ExportApprovalRequestPdfAsync(int approvalRequestId, CancellationToken ct = default);
    Task<byte[]> ExportAuditLogsExcelAsync(string? actionType, string? entityType, int? entityId,
        int? performedBy, DateTime? from, DateTime? to, CancellationToken ct = default);
}
