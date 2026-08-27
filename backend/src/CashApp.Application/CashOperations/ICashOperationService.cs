using CashApp.Application.Common.Models;
using CashApp.Application.CashOperations.Dtos;

namespace CashApp.Application.CashOperations;

public interface ICashOperationService
{
    Task<PagedResponse<CashOperationListItemDto>> ListAsync(CashOperationFilterDto filter, CancellationToken ct = default);
    Task<CashOperationDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<CashOperationDetailDto> CreateAsync(CreateCashOperationDto dto, CancellationToken ct = default);
    Task<CashOperationDetailDto> UpdateAsync(int id, UpdateCashOperationDto dto, CancellationToken ct = default);
    Task CancelAsync(int id, CancelCashOperationDto dto, CancellationToken ct = default);
}
