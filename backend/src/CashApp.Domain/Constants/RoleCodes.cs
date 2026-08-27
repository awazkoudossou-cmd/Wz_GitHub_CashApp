namespace CashApp.Domain.Constants;

public static class RoleCodes
{
    public const string Admin = "ADMIN";
    public const string Supervisor = "SUPERVISOR";
    public const string Cashier = "CASHIER";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Supervisor, Cashier };
}
