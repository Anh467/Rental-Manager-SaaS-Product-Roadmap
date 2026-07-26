using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;

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

    Task SaveAsync(
        OrganizationUser membership,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
