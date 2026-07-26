using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Org;

/// <summary>
/// An organization's own custom field definition. Distinct from
/// <see cref="Dbo.Field"/>, which is the global field template catalogue.
/// </summary>
[Table(nameof(Field), Schema = DatabaseConstant.Schema.ORG)]
public class Field :
    IDefinitionEntityAudit<Guid>,
    IOrganizationOwned,
    IConcurrencyAware
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string Key { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int FieldTypeId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public byte[] RowVersion { get; set; } = [];
}
