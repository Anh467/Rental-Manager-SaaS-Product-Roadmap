namespace RentalManager.Modules.Identity.Application.PlatformUsers;

/// <summary>
/// Canonical Platform User permission keys (Confluence 15.1).
/// </summary>
public static class PlatformUserPermissions
{
    public const string View = "platform.user.view";
    public const string Edit = "platform.user.edit";
    public const string Deactivate = "platform.user.deactivate";
}

/// <summary>
/// Seeded PlatformRole identifiers and keys.
/// </summary>
public static class PlatformRoleCatalog
{
    public const string SuperAdminKey = "PLATFORM_SUPER_ADMIN";

    public static readonly Guid SuperAdminId =
        Guid.Parse("6F2A8B1C-9D4E-4F3A-A7B8-1C2D3E4F5A6B");
}

public static class PlatformUserInvariants
{
    public const string ObjectName = "User";
}
