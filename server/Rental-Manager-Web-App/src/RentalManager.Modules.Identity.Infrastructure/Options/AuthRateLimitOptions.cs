namespace RentalManager.Modules.Identity.Infrastructure.Options;

public sealed class AuthRateLimitOptions
{
    public const string SectionName = "AuthRateLimiting";

    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}
