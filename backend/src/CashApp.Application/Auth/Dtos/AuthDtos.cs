namespace CashApp.Application.Auth.Dtos;

public record LoginRequestDto(string Username, string Password);

public record LoginResponseDto(
    string Token,
    DateTime ExpiresAt,
    CurrentUserDto User,
    IReadOnlyList<CurrentCashRegisterDto> CashRegisters,
    IReadOnlyList<FeatureDto> Features,
    string AppMode);

public record CurrentUserDto(
    int Id,
    string Username,
    string FullName,
    string RoleCode,
    bool IsActive);

public record CurrentCashRegisterDto(
    int Id,
    string Code,
    string Name,
    string CurrencyCode,
    bool IsActive);

public record FeatureDto(string FeatureCode, string FeatureName, bool IsEnabled);
