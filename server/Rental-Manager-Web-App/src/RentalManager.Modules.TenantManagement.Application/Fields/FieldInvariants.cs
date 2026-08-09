using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Core.Enums;

namespace RentalManager.Modules.TenantManagement.Application.Fields;

/// <summary>
/// The rules that belong to fields and nowhere else. Kept out of the common
/// persistence layer on purpose: another entity reusing the field CRUD shape
/// will have its own rules, not these.
/// </summary>
public static class FieldInvariants
{
    /// <summary>
    /// Whether a field type id is known in the <see cref="EFieldType"/> catalogue
    /// that mirrors <c>[dbo].[FieldType]</c> seed data. Create/update still
    /// verifies the row exists in the database.
    /// </summary>
    public static bool IsKnownFieldType(int fieldTypeId)
    {
        return Enum.IsDefined(typeof(EFieldType), fieldTypeId);
    }

    /// <summary>
    /// Selection and MultiSelect own options; other types must not carry any.
    /// </summary>
    public static bool RequiresOptions(int fieldTypeId)
    {
        return fieldTypeId is (int)EFieldType.Selection or (int)EFieldType.MultiSelect;
    }

    public static string ObjectName => MessageCode.ObjectName.Field;
}
