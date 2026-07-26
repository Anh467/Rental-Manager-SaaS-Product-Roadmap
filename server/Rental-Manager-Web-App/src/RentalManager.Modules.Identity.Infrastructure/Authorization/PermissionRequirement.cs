using Microsoft.AspNetCore.Authorization;

namespace RentalManager.Modules.Identity.Infrastructure.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
    }

    public string Permission { get; }
}

/// <summary>
/// Declares the permission an action needs. The policy name is the permission
/// key itself, resolved dynamically by <see cref="PermissionAuthorizationPolicyProvider"/>.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true)]
public sealed class RequiresPermissionAttribute : AuthorizeAttribute
{
    public RequiresPermissionAttribute(string permission)
        : base(permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
    }

    public string Permission { get; }
}
