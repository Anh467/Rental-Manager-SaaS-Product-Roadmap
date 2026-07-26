using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Org;

/// <summary>
/// Membership of a global user in one organization, with the role that applies
/// inside that organization. Composite key is
/// (<see cref="OrganizationId"/>, <see cref="UserId"/>).
/// </summary>
[Table(nameof(OrganizationUser), Schema = DatabaseConstant.Schema.ORG)]
public class OrganizationUser : IOrganizationOwned
{
    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
