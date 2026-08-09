using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;

/// <summary>
/// One active organization membership discovered at login time (RLS bypassed).
/// </summary>
public sealed record ActiveOrganizationMembership(
    Guid OrganizationId,
    string Name,
    Guid RoleId,
    DateTimeOffset CreatedAt);

/// <summary>
/// Persistence for organization membership. One user may have many memberships;
/// within a single organization the membership is unique by user.
/// </summary>
public interface IOrganizationUserRepository
{
    Task<OrganizationUser?> FindActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetActiveRoleIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every active membership for a user. Used only by login to resolve
    /// organization context without a client-supplied organization id.
    /// </summary>
    Task<IReadOnlyList<ActiveOrganizationMembership>> ListActiveMembershipsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        OrganizationUser membership,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
