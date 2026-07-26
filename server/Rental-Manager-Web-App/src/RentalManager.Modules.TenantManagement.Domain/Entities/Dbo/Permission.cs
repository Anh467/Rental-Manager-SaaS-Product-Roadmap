using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

[Table(nameof(Permission), Schema = DatabaseConstant.Schema.DBO)]
public class Permission : IEntity<int>, IDefinition
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Key { get; set; }

    public string? Description { get; set; }

    public required string Module { get; set; }

    public bool IsActive { get; set; } = true;
}
