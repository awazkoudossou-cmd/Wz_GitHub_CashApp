using CashApp.Application.Categories.Dtos;

namespace CashApp.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListItemDto>> ListAsync(CancellationToken ct = default);
    Task<CategoryDetailDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default);
    Task<CategoryDetailDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, UpdateCategoryStatusDto dto, CancellationToken ct = default);
}
