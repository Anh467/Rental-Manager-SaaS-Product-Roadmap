using System.ComponentModel.DataAnnotations.Schema;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

/// <summary>
/// Global identity. Organization membership is stored in
/// <c>[org].[OrganizationUser]</c>, so one user may belong to many organizations.
/// </summary>
[Table(nameof(User), Schema = DatabaseConstant.Schema.DBO)]
public class User : IEntityAudit<Guid>, IConcurrencyAware
{
    public Guid Id { get; set; }

    public required string UserName { get; set; }

    public required string NormalizedUserName { get; set; }

    public required string Email { get; set; }

    public required string NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; } = true;

    public required string DisplayName { get; set; }

    /// <summary>
    /// Retired credential columns. Authentication is delegated to the configured
    /// external provider through <c>[dbo].[UserIdentity]</c>, so the runtime
    /// never reads or writes these; they exist only until the historical data is
    /// dropped by an explicit migration.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <inheritdoc cref="PasswordHash"/>
    public string? PasswordSalt { get; set; }

    public required string SecurityStamp { get; set; }

    public required string ConcurrencyStamp { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public bool LockoutEnabled { get; set; } = true;

    public int AccessFailedCount { get; set; }

    public Guid? GlobalRoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public byte[] RowVersion { get; set; } = [];
}
