using CashApp.Application.CategoryGroups.Dtos;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.CategoryGroups;

public class CategoryGroupService : ICategoryGroupService
{
    private readonly IAppDbContext _db;

    public CategoryGroupService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryGroupDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.CategoryGroups.AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .Select(g => new CategoryGroupDto(g.Id, g.Name))
            .ToListAsync(ct);
    }

    public async Task<CategoryGroup> FindOrCreateAsync(string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        var existing = await _db.CategoryGroups.FirstOrDefaultAsync(
            g => g.Name.ToLower() == trimmed.ToLower(), ct);
        if (existing is not null) return existing;

        var entity = new CategoryGroup { Name = trimmed, IsActive = true };
        _db.CategoryGroups.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }
}
