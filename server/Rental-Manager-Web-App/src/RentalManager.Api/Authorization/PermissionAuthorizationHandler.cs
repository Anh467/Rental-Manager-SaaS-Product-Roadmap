using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RentalManager.Api.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;

namespace RentalManager.Api.Authorization;

/// <summary>
/// Resolves the caller's permissions for the organization currently bound to the
/// request. Missing identity or permission fails closed (403). Infrastructure
/// failures propagate so they become 500 via the exception middleware.
/// </summary>
public sealed class PermissionAuthorizationHandler :
    AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionReader _permissionReader;
    private readonly IOrganizationContext _organizationContext;
    private readonly IUserRepository _users;
    private readonly IRolePermissionRepository _globalRolePermissions;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private IReadOnlySet<string>? _cachedPermissions;

    public PermissionAuthorizationHandler(
        IPermissionReader permissionReader,
        IOrganizationContext organizationContext,
        IUserRepository users,
        IRolePermissionRepository globalRolePermissions,
        IHttpContextAccessor httpContextAccessor)
    {
        _permissionReader = permissionReader;
        _organizationContext = organizationContext;
        _users = users;
        _globalRolePermissions = globalRolePermissions;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (_organizationContext.UserId is not Guid userId)
        {
            return;
        }

        CancellationToken cancellationToken =
            _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        // The handler is scoped per request, so several [RequiresPermission]
        // checks on one request share a single database round trip.
        if (_organizationContext.HasOrganization)
        {
            _cachedPermissions ??= await _permissionReader.GetPermissionKeysAsync(
                userId,
                cancellationToken);
        }
        else if (context.User.FindFirst(JwtClaimNames.Scope)?.Value == "global")
        {
            var user = await _users.GetAsync(userId, cancellationToken);
            if (user?.GlobalRoleId is not Guid roleId)
            {
                return;
            }

            _cachedPermissions ??= await _globalRolePermissions
                .GetPermissionKeysByRoleAsync(roleId, cancellationToken);
        }
        else
        {
            return;
        }

        if (_cachedPermissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
