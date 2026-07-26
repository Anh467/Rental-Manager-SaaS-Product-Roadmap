using System.ComponentModel.DataAnnotations;

namespace RentalManager.Api.Security;

public sealed class BootstrapAdminOptions : IValidatableObject
{
    public const string SectionName = "BootstrapAdmin";

    private static readonly HashSet<string> RejectedEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "your-admin@email.com"
    };

    private static readonly HashSet<string> RejectedPasswords = new(StringComparer.Ordinal)
    {
        "YourStrongPassword"
    };

    public bool Enabled { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
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

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult(
                "BootstrapAdmin:Password is required when BootstrapAdmin:Enabled is true.",
                [nameof(Password)]);
        }
        else if (RejectedPasswords.Contains(Password))
        {
            yield return new ValidationResult(
                "BootstrapAdmin:Password must not use a placeholder value.",
                [nameof(Password)]);
        }
    }
}
