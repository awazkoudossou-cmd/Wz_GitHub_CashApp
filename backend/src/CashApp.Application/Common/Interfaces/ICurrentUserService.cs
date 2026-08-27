namespace CashApp.Application.Common.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    string? RoleCode { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string roleCode);
}
