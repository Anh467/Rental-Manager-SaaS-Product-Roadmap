using Microsoft.AspNetCore.Identity;

namespace RentalManager.Modules.Identity.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public Guid? GlobalRoleId { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Legacy PBKDF2 salt. Null after Identity rehash.
    /// </summary>
    public string? PasswordSalt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
