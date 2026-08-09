namespace RentalManager.Modules.Identity.Application.Abstractions;

/// <summary>
/// The authentication scheme to challenge to start a sign-in, plus the local
/// path the browser should land on once the provider has redirected back.
/// </summary>
public sealed record ExternalLoginChallenge(string Scheme, string RedirectUri);

/// <summary>
/// Describes how this deployment starts an external sign-in, so the controller
/// never needs to read provider configuration or decide which scheme to use.
/// </summary>
public interface IExternalLoginChallengeFactory
{
    /// <summary>
    /// Whether a provider is configured well enough to be challenged.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Builds the challenge. <paramref name="returnUrl"/> is a caller hint and is
    /// only honoured when it is a local path, so a sign-in can never be used to
    /// bounce a browser to an external site.
    /// </summary>
    ExternalLoginChallenge Create(string? returnUrl);
}
