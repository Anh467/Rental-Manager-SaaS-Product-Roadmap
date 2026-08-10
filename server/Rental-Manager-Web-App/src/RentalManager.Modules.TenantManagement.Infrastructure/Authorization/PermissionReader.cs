using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Authorization;

/// <summary>
/// Resolves membership and permissions for the organization currently bound to
/// the session. Membership comes from <c>[org].[StaffMembership]</c>; effective
/// permissions are the union of active <c>[org].[StaffRole]</c> rows mapped to
/// active organization roles.
/// </summary>
public sealed class PermissionReader : IPermissionReader
{
    private readonly IStaffMembershipRepository _staffMemberships;

    public PermissionReader(IStaffMembershipRepository staffMemberships)
    {
        ArgumentNullException.ThrowIfNull(staffMemberships);
        _staffMemberships = staffMemberships;
    }

    public async Task<bool> IsActiveMemberAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _staffMemberships.GetActiveStaffMembershipIdAsync(
            userId,
            cancellationToken) is not null;
    }

    public async Task<IReadOnlySet<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> keys =
            await _staffMemberships.GetPermissionKeysAsync(
                userId,
                cancellationToken);

        return keys.ToHashSet(StringComparer.Ordinal);
    }
}
