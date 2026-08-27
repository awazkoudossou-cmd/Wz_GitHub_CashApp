using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Access;

public class CashRegisterAccessService : ICashRegisterAccessService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CashRegisterAccessService(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> CanAccessAsync(int userId, int cashRegisterId, CancellationToken ct = default)
    {
        // ADMIN et SUPERVISOR ont accès à toutes les caisses.
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return false;
        if (user.RoleCode == RoleCodes.Admin || user.RoleCode == RoleCodes.Supervisor) return true;

        return await _db.UserCashRegisters.AsNoTracking()
            .AnyAsync(uc => uc.UserId == userId && uc.CashRegisterId == cashRegisterId, ct);
    }

    public async Task EnsureCanAccessAsync(int cashRegisterId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        if (!await CanAccessAsync(userId, cashRegisterId, ct))
            throw new ForbiddenException($"Accès à la caisse #{cashRegisterId} refusé.");
    }

    public async Task<IReadOnlyList<int>> GetAccessibleRegisterIdsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Non authentifié.");
        var isAdminOrSup = _currentUser.IsInRole(RoleCodes.Admin) || _currentUser.IsInRole(RoleCodes.Supervisor);
        if (isAdminOrSup)
        {
            return await _db.CashRegisters.AsNoTracking().Select(r => r.Id).ToListAsync(ct);
        }
        return await _db.UserCashRegisters.AsNoTracking()
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.CashRegisterId)
            .ToListAsync(ct);
    }
}
