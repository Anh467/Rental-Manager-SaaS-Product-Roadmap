using System.ComponentModel.DataAnnotations;

namespace RentalManager.Modules.Identity.Infrastructure.Bootstrap;

/// <summary>
/// The controlled way to give a new deployment its first global administrator.
/// It describes an external identity to admit, never a credential: there is no
/// password to configure, leak or rotate.
/// </summary>
public sealed class BootstrapAdminOptions : IValidatableObject
{
    public const string SectionName = "BootstrapAdmin";

    private static readonly HashSet<string> RejectedEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "your-admin@email.com"
    };

    public bool Enabled { get; set; }

    /// <summary>
    /// The provider key the administrator will sign in with. Must be one of
    /// <c>Authentication:External:AllowedProviders</c>.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// The provider's stable subject identifier for the administrator. This is
    /// the only identity key; email is a profile attribute.
    /// </summary>
    public string? Subject { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Provider))
        {
            yield return new ValidationResult(
                "BootstrapAdmin:Provider is required when BootstrapAdmin:Enabled is true.",
                [nameof(Provider)]);
        }

        if (string.IsNullOrWhiteSpace(Subject))
        {
            yield return new ValidationResult(
                "BootstrapAdmin:Subject is required when BootstrapAdmin:Enabled is true.",
                [nameof(Subject)]);
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult(
                "BootstrapAdmin:Email is required when BootstrapAdmin:Enabled is true.",
                [nameof(Email)]);
        }
        else if (RejectedEmails.Contains(Email.Trim()))
        {
            yield return new ValidationResult(
                "BootstrapAdmin:Email must not use a placeholder value.",
                [nameof(Email)]);
        }
    }
}
