using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RentalManager.Modules.TenantManagement.Core.Validation;

namespace RentalManager.Modules.Identity.Infrastructure.Authorization;

/// <summary>
/// Turns a permission key used as a policy name into a policy, so adding a
/// permission needs a seed row and an attribute rather than startup wiring.
/// </summary>
public sealed class PermissionAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private static readonly DefinitionKeyAttribute DefinitionKey = new();
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackProvider.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        AuthorizationPolicy? explicitPolicy =
            await _fallbackProvider.GetPolicyAsync(policyName);

        if (explicitPolicy is not null)
        {
            return explicitPolicy;
        }

        if (!DefinitionKey.IsValid(policyName))
        {
            return null;
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}
