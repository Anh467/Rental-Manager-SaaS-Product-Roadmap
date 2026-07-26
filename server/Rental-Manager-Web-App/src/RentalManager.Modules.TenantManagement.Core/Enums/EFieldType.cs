namespace RentalManager.Modules.TenantManagement.Core.Enums;

/// <summary>
/// Field type identifiers. <c>[dbo].[FieldType]</c> is the source of truth for
/// these values, so the numbers must stay in step with its seed data.
/// </summary>
public enum EFieldType
{
    Text = 1,
    Number = 2,
    Date = 3,
    Boolean = 4,
    Selection = 5,
    MultiSelect = 6,
}
