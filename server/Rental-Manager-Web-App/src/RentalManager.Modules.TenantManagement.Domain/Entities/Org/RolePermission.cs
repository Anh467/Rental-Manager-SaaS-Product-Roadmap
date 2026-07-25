using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Org;

[Table(nameof(RolePermission), Schema = DatabaseConstant.Schema.ORG)]
public class RolePermission : ILink, IOrganizationOwned
{
    public Guid OrganizationId { get; set; }

    public Guid RoleId { get; set; }

    public int PermissionId { get; set; }
}
