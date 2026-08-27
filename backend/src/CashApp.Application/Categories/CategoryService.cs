using CashApp.Application.CategoryGroups;
using CashApp.Application.Categories.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Categories;

public class CategoryService : ICategoryService
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ICategoryGroupService _groups;

    public CategoryService(IAppDbContext db, IDateTimeProvider clock, ICategoryGroupService groups)
    {
        _db = db;
        _clock = clock;
        _groups = groups;
    }

    public async Task<IReadOnlyList<CategoryListItemDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .Include(c => c.Group)
            .OrderBy(c => c.Code)
            .Select(c => new CategoryListItemDto(c.Id, c.Code, c.Label, c.Direction, c.IsActive, c.CreatedAt,
                c.GroupId, c.Group != null ? c.Group.Name : null))
            .ToListAsync(ct);
    }

    public async Task<CategoryDetailDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _db.Categories.AnyAsync(c => c.Code == code, ct))
            throw new BusinessRuleException("CATEGORY_CODE_EXISTS", $"Le code '{code}' est déjà utilisé.");

        var group = await _groups.FindOrCreateAsync(dto.GroupName, ct);

        var entity = new Category
        {
            Code = code,
            Label = dto.Label.Trim(),
            Direction = dto.Direction,
            IsActive = true,
            GroupId = group.Id
        };
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetDetailAsync(entity.Id, ct);
    }

    public async Task<CategoryDetailDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        var group = await _groups.FindOrCreateAsync(dto.GroupName, ct);

        entity.Label = dto.Label.Trim();
        entity.Direction = dto.Direction;
        entity.GroupId = group.Id;
        entity.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetDetailAsync(entity.Id, ct);
    }

    public async Task UpdateStatusAsync(int id, UpdateCategoryStatusDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Category), id);
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CategoryDetailDto> GetDetailAsync(int id, CancellationToken ct)
    {
        var c = await _db.Categories.AsNoTracking().Include(x => x.Group)
            .FirstAsync(x => x.Id == id, ct);
        return new CategoryDetailDto(c.Id, c.Code, c.Label, c.Direction, c.IsActive, c.CreatedAt, c.UpdatedAt,
            c.GroupId, c.Group?.Name);
    }
}
