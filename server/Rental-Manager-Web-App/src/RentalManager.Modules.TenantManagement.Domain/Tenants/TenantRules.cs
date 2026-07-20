namespace RentalManager.Modules.TenantManagement.Domain.Tenants;

public static class TenantRules
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 200;

    public static string NormalizeCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        string normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length > CodeMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(code),
                $"Tenant code cannot exceed {CodeMaxLength} characters.");
        }

        return normalized;
    }

    public static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string normalized = name.Trim();

        if (normalized.Length > NameMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(name),
                $"Tenant name cannot exceed {NameMaxLength} characters.");
        }

        return normalized;
    }
}
