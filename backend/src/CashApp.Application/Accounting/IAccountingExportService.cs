using CashApp.Application.Accounting.Dtos;
using CashApp.Application.Common.Models;

namespace CashApp.Application.Accounting;

public record AccountingExportResult(byte[] Content, string ContentType, string FileName);

public interface IAccountingExportService
{
    Task<AccountingExportPreviewDto> PreviewExportAsync(AccountingEntryFilterDto filter, CancellationToken ct = default);

    Task<AccountingExportResult> ExportGenerationAsync(int generationId, CancellationToken ct = default);
    Task<AccountingExportResult> ExportEntriesAsync(AccountingEntryFilterDto filter, CancellationToken ct = default);
    Task<AccountingExportResult> ReexportAsync(int logId, CancellationToken ct = default);

    Task<PagedResponse<AccountingExportLogDto>> ListLogsAsync(AccountingExportLogFilterDto filter, CancellationToken ct = default);
    Task<AccountingExportDetailDto> GetLogDetailAsync(int logId, CancellationToken ct = default);
    Task<AccountingExportResult> DownloadLogAsync(int logId, CancellationToken ct = default);
    Task DeleteExportAsync(int logId, CancellationToken ct = default);
}
