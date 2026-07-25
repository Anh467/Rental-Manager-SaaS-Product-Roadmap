namespace RentalManager.Modules.TenantManagement.Application.Fields;

/// <summary>
/// Permission codes for field management, matching the seeded rows in
/// <c>[dbo].[Permission]</c>.
/// </summary>
public static class FieldPermissions
{
    public const string View = "field.view";
    public const string Add = "field.add";
    public const string Edit = "field.edit";
    public const string Delete = "field.delete";
}
