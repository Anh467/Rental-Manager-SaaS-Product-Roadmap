using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace RentalManager.Api.Authorization;

/// <summary>
/// Turns a permission code used as a policy name into a policy, so adding a
/// permission needs a seed row and an attribute rather than startup wiring.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackProvider.GetFallbackPolicyAsync();
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        AuthorizationPolicy? explicitPolicy =
            await _fallbackProvider.GetPolicyAsync(policyName);

        if (explicitPolicy is not null)
        {
            return explicitPolicy;
        }

        // Permission codes are dotted, which is what distinguishes them from an
        // ordinary named policy.
        if (!policyName.Contains('.', StringComparison.Ordinal))
        {
            return null;
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}
