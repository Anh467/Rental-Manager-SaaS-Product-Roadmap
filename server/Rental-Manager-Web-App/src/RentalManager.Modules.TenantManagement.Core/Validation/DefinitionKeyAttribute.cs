using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace RentalManager.Modules.TenantManagement.Core.Validation;

/// <summary>
/// Validates a stable definition key without altering the supplied value.
/// Requiredness and length remain explicit on the concrete request property.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed partial class DefinitionKeyAttribute : ValidationAttribute
{
    public DefinitionKeyAttribute()
        : base(
            "The key may contain only lowercase ASCII letters, digits, underscores, dots, and hyphens.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is null ||
               value is string key && DefinitionKeyRegex().IsMatch(key);
    }

    // Confluence 15.1 uses module.action / platform.module.action and hyphenated
    // segments such as property-type.view. Underscore keys remain valid for
    // already-seeded TenantManagement permissions.
    [GeneratedRegex("^[a-z0-9]+([._-][a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex DefinitionKeyRegex();
}
