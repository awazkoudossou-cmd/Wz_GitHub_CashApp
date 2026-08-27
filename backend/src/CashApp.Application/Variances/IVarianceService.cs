using CashApp.Application.Common.Models;
using CashApp.Application.Variances.Dtos;
using CashApp.Domain.Entities.V2;

namespace CashApp.Application.Variances;

public interface IVarianceService
{
    Task<PagedResponse<VarianceCaseListItemDto>> ListAsync(VarianceFilterDto filter, CancellationToken ct = default);
    Task<VarianceCaseDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<VarianceCaseDetailDto> AddJustificationAsync(int id, CreateVarianceJustificationDto dto, CancellationToken ct = default);
    Task<VarianceCaseDetailDto?> FindBySessionAsync(int cashSessionId, CancellationToken ct = default);

    Task<VarianceCase> CreateForSessionAsync(int cashSessionId, decimal varianceAmount, string currencyCode, CancellationToken ct = default);
}
