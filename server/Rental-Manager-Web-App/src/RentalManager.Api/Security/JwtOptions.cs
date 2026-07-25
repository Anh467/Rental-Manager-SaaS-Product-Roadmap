namespace RentalManager.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 60;
}

public static class JwtClaimNames
{
    public const string Subject = "sub";

    /// <summary>
    /// The organization the token was issued for. Switching organization means
    /// obtaining a new token, which forces the membership check to run again.
    /// </summary>
    public const string OrganizationId = "organization_id";
}
