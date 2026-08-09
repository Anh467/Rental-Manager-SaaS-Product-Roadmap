using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Org;

/// <summary>
/// Assigns an organization role to a staff membership. A membership may have
/// many roles; effective permissions are the union of active roles.
/// </summary>
[Table(nameof(StaffRole), Schema = DatabaseConstant.Schema.ORG)]
public class StaffRole : IOrganizationOwned
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid StaffMembershipId { get; set; }

    public Guid RoleId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
