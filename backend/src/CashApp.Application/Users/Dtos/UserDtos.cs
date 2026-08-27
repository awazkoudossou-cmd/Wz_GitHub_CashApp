namespace CashApp.Application.Users.Dtos;

public record UserListItemDto(
    int Id,
    string Username,
    string FullName,
    string RoleCode,
    bool IsActive,
    DateTime CreatedAt);

public record UserDetailDto(
    int Id,
    string Username,
    string FullName,
    string RoleCode,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<int> CashRegisterIds);

public record CreateUserDto(
    string Username,
    string FullName,
    string Password,
    string RoleCode,
    IReadOnlyList<int>? CashRegisterIds);

public record UpdateUserDto(
    string FullName,
    string RoleCode,
    IReadOnlyList<int>? CashRegisterIds);

public record UpdateUserStatusDto(bool IsActive);

public record ResetPasswordDto(string NewPassword);
