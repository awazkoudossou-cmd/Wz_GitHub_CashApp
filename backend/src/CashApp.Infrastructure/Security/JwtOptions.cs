namespace CashApp.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CashApp";
    public string Audience { get; set; } = "CashApp.Clients";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 480;
}
