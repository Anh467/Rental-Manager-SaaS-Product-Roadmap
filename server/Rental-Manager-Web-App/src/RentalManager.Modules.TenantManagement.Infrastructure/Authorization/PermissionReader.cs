using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Authorization;

/// <summary>
/// Resolves membership and permissions for the organization currently bound to
/// the session. Membership comes from <c>[org].[OrganizationUser]</c>, so one
/// user may belong to many organizations with a different role in each.
/// </summary>
public sealed class PermissionReader : IPermissionReader
{
    private readonly IOrganizationUserRepository _organizationUsers;
    private readonly IRolePermissionRepository _rolePermissions;

    public PermissionReader(
        IOrganizationUserRepository organizationUsers,
        IRolePermissionRepository rolePermissions)
    {
        ArgumentNullException.ThrowIfNull(organizationUsers);
        ArgumentNullException.ThrowIfNull(rolePermissions);

        _organizationUsers = organizationUsers;
        _rolePermissions = rolePermissions;
    }

    public async Task<bool> IsActiveMemberAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _organizationUsers.GetActiveRoleIdAsync(
            userId,
            cancellationToken) is not null;
    }

    public async Task<IReadOnlySet<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Guid? roleId = await _organizationUsers.GetActiveRoleIdAsync(
            userId,
            cancellationToken);

        if (roleId is null)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        IReadOnlyCollection<string> keys =
            await _rolePermissions.GetPermissionKeysByRoleAsync(
                roleId.Value,
                cancellationToken);

        return keys.ToHashSet(StringComparer.Ordinal);
    }
}
