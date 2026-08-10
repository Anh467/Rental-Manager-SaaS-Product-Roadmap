namespace RentalManager.BuildingBlocks.Tenancy.Abstractions;

/// <summary>
/// The trusted organization context for the current unit of work. It is
/// resolved at the authenticated application boundary and never from a request
/// body, query string or arbitrary header.
/// Trusted fields: UserId, OrganizationId, StaffMembershipId, CorrelationId.
/// </summary>
public interface IOrganizationContext
{
    Guid? OrganizationId { get; }

    Guid? UserId { get; }

    /// <summary>
    /// Verified staff membership id for the bound organization, when the session
    /// is organization-scoped.
    /// </summary>
    Guid? StaffMembershipId { get; }

    string? CorrelationId { get; }

    bool HasOrganization { get; }
}
