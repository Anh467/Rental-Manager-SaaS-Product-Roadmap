using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

/// <summary>
/// Global identity. Organization membership is stored in
/// <c>[org].[OrganizationUser]</c>, so one user may belong to many organizations.
/// </summary>
[Table(nameof(User), Schema = DatabaseConstant.Schema.DBO)]
public class User : IEntityAudit<Guid>
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public required string NormalizedEmail { get; set; }

    public required string DisplayName { get; set; }

    public required string PasswordHash { get; set; }

    public required string PasswordSalt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
