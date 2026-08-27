using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Users.Dtos;
using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Users;

public class UserService : IUserService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public UserService(IAppDbContext db, IPasswordHasher hasher, IDateTimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<IReadOnlyList<UserListItemDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserListItemDto(u.Id, u.Username, u.FullName, u.RoleCode, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<UserDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserCashRegisters)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        return Map(user);
    }

    public async Task<UserDetailDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        var username = dto.Username.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            throw new BusinessRuleException("USERNAME_EXISTS", $"Le username '{username}' est déjà utilisé.");

        var user = new User
        {
            Username = username,
            FullName = dto.FullName.Trim(),
            PasswordHash = _hasher.Hash(dto.Password),
            RoleCode = dto.RoleCode,
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        if (dto.CashRegisterIds is { Count: > 0 })
        {
            await AssignRegistersAsync(user.Id, dto.CashRegisterIds, ct);
        }

        return await GetAsync(user.Id, ct);
    }

    public async Task<UserDetailDto> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserCashRegisters)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        user.FullName = dto.FullName.Trim();
        user.RoleCode = dto.RoleCode;
        user.UpdatedAt = _clock.UtcNow;

        if (dto.CashRegisterIds is not null)
        {
            _db.UserCashRegisters.RemoveRange(user.UserCashRegisters);
            await _db.SaveChangesAsync(ct);
            await AssignRegistersAsync(user.Id, dto.CashRegisterIds, ct);
        }

        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task UpdateStatusAsync(int id, UpdateUserStatusDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        user.IsActive = dto.IsActive;
        user.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        user.PasswordHash = _hasher.Hash(dto.NewPassword);
        user.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task AssignRegistersAsync(int userId, IReadOnlyList<int> registerIds, CancellationToken ct)
    {
        var validIds = await _db.CashRegisters
            .Where(r => registerIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(ct);

        var now = _clock.UtcNow;
        foreach (var rid in validIds.Distinct())
        {
            _db.UserCashRegisters.Add(new UserCashRegister
            {
                UserId = userId,
                CashRegisterId = rid,
                AssignedAt = now
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private static UserDetailDto Map(User u) =>
        new(u.Id, u.Username, u.FullName, u.RoleCode, u.IsActive, u.CreatedAt, u.UpdatedAt,
            u.UserCashRegisters.Select(r => r.CashRegisterId).ToList());
}
