namespace RentalManager.BuildingBlocks.Tenancy.Abstractions;

/// <summary>
/// The trusted organization context for the current unit of work. It is
/// resolved at the authenticated application boundary and never from a request
/// body, query string or arbitrary header.
/// </summary>
public interface IOrganizationContext
{
    Guid? OrganizationId { get; }

    Guid? UserId { get; }

    string? CorrelationId { get; }

    bool HasOrganization { get; }
}
