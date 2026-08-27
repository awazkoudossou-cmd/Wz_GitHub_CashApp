using CashApp.Application.Common.Interfaces;

namespace CashApp.Application.Tests.Fakes;

public class FakeClock : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new DateTime(2026, 6, 24, 9, 0, 0, DateTimeKind.Utc);
    public DateTime Now => UtcNow.ToLocalTime();
    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
}

public class FakeCurrentUser : ICurrentUserService
{
    public int? UserId { get; set; } = 1;
    public string? Username { get; set; } = "admin";
    public string? RoleCode { get; set; } = "ADMIN";
    public bool IsAuthenticated => UserId.HasValue;
    private string[] _roles = new[] { "ADMIN" };

    public void SetRole(string role)
    {
        RoleCode = role;
        _roles = new[] { role };
    }

    public bool IsInRole(string roleCode) => _roles.Contains(roleCode);
}
