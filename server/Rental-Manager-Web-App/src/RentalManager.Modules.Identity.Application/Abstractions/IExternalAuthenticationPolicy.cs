namespace RentalManager.Modules.Identity.Application.Abstractions;

/// <summary>
/// The deployment's onboarding policy for external identities. Keeping this an
/// abstraction is what stops the use case from reading configuration or naming
/// a concrete identity provider.
/// </summary>
public interface IExternalAuthenticationPolicy
{
    /// <summary>
    /// Whether the provider key is allow-listed for this deployment. A provider
    /// outside the allow-list can never map or provision a user.
    /// </summary>
    bool IsProviderAllowed(string provider);

    /// <summary>
    /// Whether an allow-listed provider may create a user on first login.
    /// </summary>
    bool AllowsFirstLoginProvisioning(string provider);
}
