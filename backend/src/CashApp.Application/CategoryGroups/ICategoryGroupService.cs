using CashApp.Application.CategoryGroups.Dtos;
using CashApp.Domain.Entities;

namespace CashApp.Application.CategoryGroups;

public interface ICategoryGroupService
{
    Task<IReadOnlyList<CategoryGroupDto>> ListAsync(CancellationToken ct = default);

    // Retourne le groupe existant (recherche insensible à la casse) ou en crée un nouveau.
    Task<CategoryGroup> FindOrCreateAsync(string name, CancellationToken ct = default);
}
