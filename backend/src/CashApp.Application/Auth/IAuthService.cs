using CashApp.Application.Auth.Dtos;

namespace CashApp.Application.Auth;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<LoginResponseDto> GetCurrentContextAsync(int userId, CancellationToken ct = default);
}
