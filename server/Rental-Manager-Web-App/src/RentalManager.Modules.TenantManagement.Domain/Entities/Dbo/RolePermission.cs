using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

[Table(nameof(RolePermission), Schema = DatabaseConstant.Schema.DBO)]
public class RolePermission : ILink
{
    public Guid RoleId { get; set; }

    public int PermissionId { get; set; }
}
