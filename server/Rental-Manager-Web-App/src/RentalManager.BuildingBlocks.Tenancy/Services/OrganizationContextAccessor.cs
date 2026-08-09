using RentalManager.BuildingBlocks.Tenancy.Abstractions;

namespace RentalManager.BuildingBlocks.Tenancy.Services;

/// <summary>
/// Scoped, mutable holder for the organization context. Only the authenticated
/// application boundary is allowed to populate it; everything downstream reads
/// it through <see cref="IOrganizationContext"/>.
/// </summary>
public sealed class OrganizationContextAccessor : IOrganizationContext
{
    public Guid? OrganizationId { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? StaffMembershipId { get; private set; }

    public string? CorrelationId { get; private set; }

    public bool HasOrganization => OrganizationId is not null;

    public void SetCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        CorrelationId = correlationId;
    }

    public void SetUser(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// Binds the organization and verified staff membership the caller has
    /// already been authorized for.
    /// </summary>
    public void SetOrganization(Guid organizationId, Guid staffMembershipId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization id must not be empty.",
                nameof(organizationId));
        }

        if (staffMembershipId == Guid.Empty)
        {
            throw new ArgumentException(
                "Staff membership id must not be empty.",
                nameof(staffMembershipId));
        }

        OrganizationId = organizationId;
        StaffMembershipId = staffMembershipId;
    }
}
