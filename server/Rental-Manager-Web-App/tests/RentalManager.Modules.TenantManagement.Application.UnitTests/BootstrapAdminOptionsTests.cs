using System.ComponentModel.DataAnnotations;
using RentalManager.Modules.Identity.Infrastructure.Bootstrap;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class BootstrapAdminOptionsTests
{
    [Fact]
    public void Disabled_bootstrap_skips_identity_validation()
    {
        var options = new BootstrapAdminOptions
        {
            Enabled = false,
            Provider = "",
            Subject = "",
            Email = ""
        };

        Assert.Empty(Validate(options));
    }

    [Fact]
    public void Enabled_bootstrap_requires_provider_subject_and_email()
    {
        var options = new BootstrapAdminOptions
        {
            Enabled = true,
            Provider = " ",
            Subject = null,
            Email = " "
        };

        ValidationResult[] results = Validate(options);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(BootstrapAdminOptions.Provider)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(BootstrapAdminOptions.Subject)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(BootstrapAdminOptions.Email)));
    }

    [Fact]
    public void Enabled_bootstrap_rejects_placeholder_email()
    {
        var options = new BootstrapAdminOptions
        {
            Enabled = true,
            Provider = "oidc",
            Subject = "admin-subject",
            Email = "your-admin@email.com"
        };

        Assert.NotEmpty(Validate(options));
    }

    [Fact]
    public void Enabled_bootstrap_accepts_external_identity()
    {
        var options = new BootstrapAdminOptions
        {
            Enabled = true,
            Provider = "oidc",
            Subject = "admin-subject",
            Email = "ops@example.com",
            DisplayName = "Ops"
        };

        Assert.Empty(Validate(options));
    }

    private static ValidationResult[] Validate(BootstrapAdminOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        return results.ToArray();
    }
}
