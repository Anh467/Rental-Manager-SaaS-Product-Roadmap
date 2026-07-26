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
}
