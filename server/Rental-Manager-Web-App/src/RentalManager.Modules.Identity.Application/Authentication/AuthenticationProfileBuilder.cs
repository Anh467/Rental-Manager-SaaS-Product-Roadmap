using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;

namespace RentalManager.Modules.Identity.Application.Authentication;

public sealed class AuthenticationProfileBuilder(
    OrganizationContextAccessor organizationContextAccessor,
    IPermissionReader permissionReader,
    IPlatformPermissionReader platformPermissionReader)
{
    public async Task<AuthenticationResultDto> BuildAsync(
        AuthenticatedIdentity identity,
        string scope,
        Guid? organizationId,
        Guid? staffMembershipId,
        CancellationToken cancellationToken)
    {
        organizationContextAccessor.SetUser(identity.UserId);

        if (organizationId is Guid orgId && staffMembershipId is Guid membershipId)
        {
            organizationContextAccessor.SetOrganization(orgId, membershipId);
        }

        if (string.Equals(scope, IdentityClaimNames.ScopeGlobal, StringComparison.Ordinal))
        {
            return await BuildGlobalAsync(identity, cancellationToken);
        }

        IReadOnlySet<string> orgPermissions =
            await permissionReader.GetPermissionKeysAsync(
                identity.UserId,
                cancellationToken);

        return new AuthenticationResultDto(
            identity.UserId,
            identity.DisplayName,
            identity.Email,
            identity.IsActive,
            IdentityClaimNames.ScopeOrganization,
            organizationId,
            null,
            orgPermissions.ToArray());
    }

    public CurrentUserDto ToCurrentUser(AuthenticationResultDto authentication) =>
        new(
            authentication.Id,
            authentication.Name,
            authentication.Email,
            authentication.IsActive,
            authentication.Scope,
            authentication.OrganizationId,
            authentication.Role,
            authentication.Permissions);

    private async Task<AuthenticationResultDto> BuildGlobalAsync(
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken)
    {
        IReadOnlySet<string> permissions =
            await platformPermissionReader.GetPermissionKeysAsync(
                identity.UserId,
                cancellationToken);

        PlatformRoleSummary? platformRole =
            await platformPermissionReader.GetPrimaryRoleAsync(
                identity.UserId,
                cancellationToken);

        RoleSummaryDto? roleSummary = platformRole is null
            ? null
            : new RoleSummaryDto(platformRole.Key, platformRole.Name);

        if (roleSummary is null && permissions.Count == 0)
        {
            throw new InvalidOperationException(
                "A global authentication profile requires an active platform role.");
        }

        return new AuthenticationResultDto(
            identity.UserId,
            identity.DisplayName,
            identity.Email,
            identity.IsActive,
            IdentityClaimNames.ScopeGlobal,
            null,
            roleSummary,
            permissions.ToArray());
    }
}
