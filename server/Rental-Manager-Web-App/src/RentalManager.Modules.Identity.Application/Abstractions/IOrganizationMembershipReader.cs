using RentalManager.Modules.Identity.Application.Contracts;

namespace RentalManager.Modules.Identity.Application.Abstractions;

public interface IOrganizationMembershipReader
{
    Task<IReadOnlyList<OrganizationOptionDto>> ListActiveOrganizationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveMemberAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the verified active staff membership id for the user in the
    /// given organization, or null when the membership is not usable.
    /// </summary>
    Task<Guid?> GetActiveStaffMembershipIdAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
