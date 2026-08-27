using CashApp.Application.Common.Models;
using CashApp.Application.Imports.Dtos;
using CashApp.Domain.Enums;

namespace CashApp.Application.Imports;

public interface IImportService
{
    Task<PagedResponse<ImportBatchListItemDto>> ListAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<ImportBatchDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<ImportBatchDetailDto> UploadAsync(ImportBatchType batchType, int? cashRegisterId, string originalFileName, Stream content, CancellationToken ct = default);
    Task<ImportPreviewDto> PreviewAsync(int id, CancellationToken ct = default);
    Task<ImportBatchDetailDto> ConfirmAsync(int id, ConfirmImportDto dto, CancellationToken ct = default);
}
