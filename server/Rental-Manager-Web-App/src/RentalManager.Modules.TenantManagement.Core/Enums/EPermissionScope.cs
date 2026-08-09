namespace RentalManager.Modules.TenantManagement.Core.Enums;

/// <summary>
/// Catalog scope for <c>dbo.Permission</c>. PlatformRolePermission may only
/// reference Platform-scoped rows; organization roles must not receive
/// platform.* permissions.
/// </summary>
public enum EPermissionScope
{
    Platform = 1,
    Organization = 2
}
