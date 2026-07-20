namespace RentalManager.Modules.Identity.Infrastructure.Options;

public sealed class IdentityProviderOptions
{
    public const string SectionName = "Identity";

    public required string Provider { get; init; }

    public string? Authority { get; init; }

    public string? Audience { get; init; }

    public string? ClientId { get; init; }
}
