using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using DboRolePermissionRepository =
    RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo.IRolePermissionRepository;

namespace RentalManager.Modules.Identity.Application.Authentication;

public sealed class AuthenticationProfileBuilder(
    OrganizationContextAccessor organizationContextAccessor,
    IPermissionReader permissionReader,
    IRoleRepository roles,
    DboRolePermissionRepository globalRolePermissions)
{
    public async Task<AuthenticationResultDto> BuildAsync(
        AuthenticatedIdentity identity,
        string scope,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        organizationContextAccessor.SetUser(identity.UserId);

        if (organizationId is Guid orgId)
        {
            organizationContextAccessor.SetOrganization(orgId);
        }

        if (string.Equals(scope, IdentityClaimNames.ScopeGlobal, StringComparison.Ordinal))
        {
            if (identity.GlobalRoleId is not Guid roleId)
            {
                throw new InvalidOperationException(
                    "A global authentication profile requires a global role.");
            }

            var role = await roles.GetAsync(roleId, cancellationToken)
                ?? throw new InvalidOperationException(
                    "The global role assigned to the user was not found.");

            IReadOnlySet<string> permissions =
                await globalRolePermissions.GetPermissionKeysByRoleAsync(
                    roleId,
                    cancellationToken);

            return new AuthenticationResultDto(
                identity.UserId,
                identity.DisplayName,
                identity.Email,
                IdentityClaimNames.ScopeGlobal,
                null,
                new RoleSummaryDto(role.Key, role.Name),
                permissions.ToArray());
        }

        IReadOnlySet<string> orgPermissions =
            await permissionReader.GetPermissionKeysAsync(
                identity.UserId,
                cancellationToken);

        return new AuthenticationResultDto(
            identity.UserId,
            identity.DisplayName,
            identity.Email,
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
            authentication.Scope,
            authentication.OrganizationId,
            authentication.Role,
            authentication.Permissions);
}
