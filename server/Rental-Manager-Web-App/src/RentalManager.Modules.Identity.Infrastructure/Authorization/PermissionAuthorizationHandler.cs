using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;

namespace RentalManager.Modules.Identity.Infrastructure.Authorization;

/// <summary>
/// Resolves the caller's permissions for the organization currently bound to the
/// request, or the union of PlatformRole permissions for a global-scope session.
/// Missing identity or permission fails closed (403). Infrastructure failures
/// propagate so they become 500 via the exception middleware.
/// </summary>
public sealed class PermissionAuthorizationHandler :
    AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionReader _permissionReader;
    private readonly IPlatformPermissionReader _platformPermissionReader;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private IReadOnlySet<string>? _cachedPermissions;

    public PermissionAuthorizationHandler(
        IPermissionReader permissionReader,
        IPlatformPermissionReader platformPermissionReader,
        IHttpContextAccessor httpContextAccessor)
    {
        _permissionReader = permissionReader;
        _platformPermissionReader = platformPermissionReader;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ClaimsPrincipalUser principal = ClaimsPrincipalUser.From(context.User);
        if (principal.UserId is not Guid userId)
        {
            return;
        }

        CancellationToken cancellationToken =
            _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        if (principal.ActiveOrganizationId is Guid)
        {
            _cachedPermissions ??= await _permissionReader.GetPermissionKeysAsync(
                userId,
                cancellationToken);
        }
        else if (string.Equals(
                     principal.Scope,
                     IdentityClaimNames.ScopeGlobal,
                     StringComparison.Ordinal))
        {
            _cachedPermissions ??= await _platformPermissionReader.GetPermissionKeysAsync(
                userId,
                cancellationToken);
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

    private sealed record ClaimsPrincipalUser(
        Guid? UserId,
        Guid? ActiveOrganizationId,
        string? Scope)
    {
        public static ClaimsPrincipalUser From(System.Security.Claims.ClaimsPrincipal principal)
        {
            Guid? userId = ReadGuid(principal, IdentityClaimNames.UserId)
                ?? ReadGuid(principal, System.Security.Claims.ClaimTypes.NameIdentifier);

            return new ClaimsPrincipalUser(
                userId,
                ReadGuid(principal, IdentityClaimNames.ActiveOrganizationId),
                principal.FindFirst(IdentityClaimNames.Scope)?.Value);
        }

        private static Guid? ReadGuid(
            System.Security.Claims.ClaimsPrincipal principal,
            string claimType)
        {
            string? raw = principal.FindFirst(claimType)?.Value;
            return Guid.TryParse(raw, out Guid value) ? value : null;
        }
    }
}
