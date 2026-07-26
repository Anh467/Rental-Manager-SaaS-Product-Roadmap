namespace RentalManager.Api.Security;

public sealed class AuthRateLimitOptions
{
    public const string SectionName = "AuthRateLimiting";

    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}
