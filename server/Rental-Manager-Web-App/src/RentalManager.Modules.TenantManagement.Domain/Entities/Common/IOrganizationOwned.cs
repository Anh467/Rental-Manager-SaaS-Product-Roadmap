namespace RentalManager.Modules.TenantManagement.Domain.Entities.Common;

/// <summary>
/// Marks an entity that lives in the shared <c>[org]</c> schema and is owned by
/// exactly one organization. The value is always assigned by the server from
/// the authenticated organization context, never from the client.
/// </summary>
public interface IOrganizationOwned
{
    Guid OrganizationId { get; set; }
}
