using Microsoft.Extensions.Options;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Options;

namespace RentalManager.Modules.Identity.Infrastructure.Authentication;

/// <summary>
/// Applies the configured allow-list. Provider keys are compared
/// case-insensitively because they are deployment configuration rather than
/// provider-issued data.
/// </summary>
public sealed class ExternalAuthenticationPolicy(
    IOptionsMonitor<ExternalAuthenticationOptions> options)
    : IExternalAuthenticationPolicy
{
    public bool IsProviderAllowed(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return false;
        }

        return options.CurrentValue.AllowedProviders.Any(allowed =>
            string.Equals(allowed?.Trim(), provider.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public bool AllowsFirstLoginProvisioning(string provider) =>
        IsProviderAllowed(provider) && options.CurrentValue.AllowFirstLoginProvisioning;
}
