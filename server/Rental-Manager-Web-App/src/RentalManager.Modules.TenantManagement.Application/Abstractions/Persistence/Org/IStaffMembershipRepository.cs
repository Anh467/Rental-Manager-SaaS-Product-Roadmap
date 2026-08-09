using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;

/// <summary>
/// One active organization membership discovered at login time (RLS bypassed).
/// </summary>
public sealed record ActiveOrganizationMembership(
    Guid StaffMembershipId,
    Guid OrganizationId,
    string Name,
    DateTimeOffset CreatedAt);

/// <summary>
/// Persistence for staff memberships. One user may have many memberships;
/// within a single organization the membership is unique by user while not
/// soft-deleted.
/// </summary>
public interface IStaffMembershipRepository
{
    Task<StaffMembership?> FindActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetActiveStaffMembershipIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every active membership for a user. Used only by login to resolve
    /// organization context without a client-supplied organization id.
    /// </summary>
    Task<IReadOnlyList<ActiveOrganizationMembership>> ListActiveMembershipsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        StaffMembership membership,
        CancellationToken cancellationToken = default);

    Task AssignRoleAsync(
        Guid staffMembershipId,
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
