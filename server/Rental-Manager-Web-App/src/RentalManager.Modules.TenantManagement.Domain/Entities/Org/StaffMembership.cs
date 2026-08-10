using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Org;

/// <summary>
/// Staff membership of a global user in one organization.
/// Status: Pending = 1, Active = 2, Inactive = 3.
/// </summary>
[Table(nameof(StaffMembership), Schema = DatabaseConstant.Schema.ORG)]
public class StaffMembership :
    IEntityAudit<Guid>,
    IOrganizationOwned,
    IConcurrencyAware
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Pending = 1, Active = 2, Inactive = 3.
    /// </summary>
    public byte Status { get; set; } = StaffMembershipStatuses.Active;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public byte[] RowVersion { get; set; } = [];
}

public static class StaffMembershipStatuses
{
    public const byte Pending = 1;

    public const byte Active = 2;

    public const byte Inactive = 3;
}
