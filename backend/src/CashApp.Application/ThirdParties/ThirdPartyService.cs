using CashApp.Application.Common.Interfaces;
using CashApp.Application.ThirdParties.Dtos;
using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.ThirdParties;

public class ThirdPartyService : IThirdPartyService
{
    private readonly IAppDbContext _db;

    public ThirdPartyService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ThirdPartyDto>> ListAsync(string? search, CancellationToken ct = default)
    {
        var q = _db.ThirdParties.AsNoTracking().Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(t => t.Name.Contains(search));
        return await q.OrderBy(t => t.Name)
            .Select(t => new ThirdPartyDto(t.Id, t.Name))
            .ToListAsync(ct);
    }

    public async Task<ThirdParty> FindOrCreateAsync(string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        var existing = await _db.ThirdParties.FirstOrDefaultAsync(
            t => t.Name.ToLower() == trimmed.ToLower(), ct);
        if (existing is not null) return existing;

        var entity = new ThirdParty { Name = trimmed, IsActive = true };
        _db.ThirdParties.Add(entity);
        // Persiste immédiatement : appelé au fil de la création/mise à jour d'une opération,
        // dont le SaveChanges est déclenché juste après par l'appelant.
        await _db.SaveChangesAsync(ct);
        return entity;
    }
}
