using RentalManager.BuildingBlocks.Tenancy.Abstractions;

namespace RentalManager.BuildingBlocks.Tenancy.Services;

/// <summary>
/// Immutable organization context for callers that have no HTTP request, such
/// as background jobs, which must pass the organization explicitly.
/// </summary>
public sealed class ExplicitOrganizationContext : IOrganizationContext
{
    public ExplicitOrganizationContext(
        Guid organizationId,
        Guid? userId = null,
        string? correlationId = null,
        Guid? staffMembershipId = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization id must not be empty.",
                nameof(organizationId));
        }

        OrganizationId = organizationId;
        UserId = userId;
        CorrelationId = correlationId;
        StaffMembershipId = staffMembershipId;
    }

    public Guid? OrganizationId { get; }

    public Guid? UserId { get; }

    public Guid? StaffMembershipId { get; }

    public string? CorrelationId { get; }

    public bool HasOrganization => true;
}
