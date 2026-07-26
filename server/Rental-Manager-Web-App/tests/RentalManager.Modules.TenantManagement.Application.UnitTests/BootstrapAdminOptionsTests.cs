using System.ComponentModel.DataAnnotations;
using RentalManager.Modules.Identity.Infrastructure.Bootstrap;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class BootstrapAdminOptionsTests
{
    [Fact]
    public void Disabled_bootstrap_skips_credential_validation()
    {
        var options = new BootstrapAdminOptions
        {
            Enabled = false,
            Email = "",
            Password = ""
        };

        Assert.Empty(Validate(options));
    }

    [Fact]
    public void Enabled_bootstrap_requires_email_and_password()
    {
        var options = new BootstrapAdminOptions
        {
            Enabled = true,
            Email = " ",
            Password = null
        };

        ValidationResult[] results = Validate(options);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(BootstrapAdminOptions.Email)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(BootstrapAdminOptions.Password)));
    }

    [Theory]
    [InlineData("your-admin@email.com", "ValidPassword123!")]
    [InlineData("admin@example.com", "YourStrongPassword")]
    public void Enabled_bootstrap_rejects_placeholder_credentials(
        string email,
        string password)
    {
        var options = new BootstrapAdminOptions
        {
            Enabled = true,
            Email = email,
            Password = password
        };

        Assert.NotEmpty(Validate(options));
    }

    [Fact]
    public void Enabled_bootstrap_accepts_non_placeholder_credentials()
    {
        var options = new BootstrapAdminOptions
        {
            Enabled = true,
            Email = "ops@example.com",
            Password = "A-Strong-Local-Only-Password1!"
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
