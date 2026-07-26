namespace RentalManager.Api.Security;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";
    public bool Enabled { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}
