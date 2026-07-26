using Microsoft.AspNetCore.Authorization;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Api.Security;

namespace RentalManager.Api.Authorization;

/// <summary>
/// Resolves the caller's permissions for the organization currently bound to the
/// request. Fails closed: without an organization context there is nothing to
/// authorize against, so the requirement is not met.
/// </summary>
public sealed class PermissionAuthorizationHandler :
    AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionReader _permissionReader;
    private readonly IOrganizationContext _organizationContext;
    private readonly IUserRepository _users;
    private readonly IRolePermissionRepository _globalRolePermissions;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    private IReadOnlySet<string>? _cachedPermissions;

    public PermissionAuthorizationHandler(
        IPermissionReader permissionReader,
        IOrganizationContext organizationContext,
        IUserRepository users,
        IRolePermissionRepository globalRolePermissions,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionReader = permissionReader;
        _organizationContext = organizationContext;
        _users = users;
        _globalRolePermissions = globalRolePermissions;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (_organizationContext.UserId is not Guid userId)
        {
            return;
        }

        try
        {
            // The handler is scoped per request, so several [RequiresPermission]
            // checks on one request share a single database round trip.
            if (_organizationContext.HasOrganization)
            {
                _cachedPermissions ??= await _permissionReader.GetPermissionKeysAsync(
                    userId, CancellationToken.None);
            }
            else if (context.User.FindFirst(JwtClaimNames.Scope)?.Value == "global")
            {
                var user = await _users.GetAsync(userId, CancellationToken.None);
                if (user?.GlobalRoleId is not Guid roleId)
                {
                    return;
                }
                _cachedPermissions ??= await _globalRolePermissions
                    .GetPermissionKeysByRoleAsync(roleId, CancellationToken.None);
            }
            else
            {
                return;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to resolve permissions for user {UserId}.",
                userId);

            return;
        }

        if (_cachedPermissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
