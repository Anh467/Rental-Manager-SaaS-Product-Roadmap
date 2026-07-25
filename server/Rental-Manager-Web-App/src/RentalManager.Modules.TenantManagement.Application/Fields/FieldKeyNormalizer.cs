namespace RentalManager.Modules.TenantManagement.Application.Fields;

/// <summary>
/// Single definition of how a field or option key is compared for uniqueness.
/// The database unique constraints are built on the normalized value, so this
/// must stay the only place the rule is expressed.
/// </summary>
public static class FieldKeyNormalizer
{
    /// <summary>
    /// Removes surrounding whitespace. This is the value stored in
    /// <c>[Key]</c> and shown back to the user.
    /// </summary>
    public static string Trim(string? key)
    {
        return key?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Trims and upper-cases with invariant culture, so uniqueness never depends
    /// on the server's locale.
    /// </summary>
    public static string Normalize(string? key)
    {
        return Trim(key).ToUpperInvariant();
    }
}
