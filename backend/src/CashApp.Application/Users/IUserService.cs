using CashApp.Application.Users.Dtos;

namespace CashApp.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserListItemDto>> ListAsync(CancellationToken ct = default);
    Task<UserDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<UserDetailDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<UserDetailDto> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, UpdateUserStatusDto dto, CancellationToken ct = default);
    Task ResetPasswordAsync(int id, ResetPasswordDto dto, CancellationToken ct = default);
}
