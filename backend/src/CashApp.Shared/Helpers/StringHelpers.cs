namespace CashApp.Shared.Helpers;

public static class StringHelpers
{
    public static string? TrimToNull(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }

    public static string ToUpperCode(this string value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant().Replace(' ', '_');
}
