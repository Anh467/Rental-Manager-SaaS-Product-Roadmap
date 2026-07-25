using RentalManager.Modules.TenantManagement.Core.Constants;
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
    /// Entity types a field can extend. A closed list, so a typo cannot silently
    /// create a new scope with its own primary field.
    /// </summary>
    public static readonly IReadOnlySet<string> SupportedTargetEntityTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Property",
            "Room",
            "Person",
            "Document"
        };

    /// <summary>
    /// Field types supported by the MVP. <c>Date</c> and <c>Selection</c> exist
    /// in <c>[dbo].[FieldType]</c> but are not accepted yet, and JSON and file
    /// types are not modelled at all.
    /// </summary>
    public static readonly IReadOnlySet<int> SupportedFieldTypeIds =
        new HashSet<int>
        {
            (int)EFieldType.Text,
            (int)EFieldType.Number,
            (int)EFieldType.Boolean,
            (int)EFieldType.MultiSelect
        };

    public static bool IsSupportedTargetEntityType(string? targetEntityType)
    {
        return targetEntityType is not null &&
               SupportedTargetEntityTypes.Contains(targetEntityType);
    }

    public static bool IsSupportedFieldType(int fieldTypeId)
    {
        return SupportedFieldTypeIds.Contains(fieldTypeId);
    }

    /// <summary>
    /// Only a MultiSelect field owns options, and a MultiSelect field is
    /// meaningless without at least one.
    /// </summary>
    public static bool RequiresOptions(int fieldTypeId)
    {
        return fieldTypeId == (int)EFieldType.MultiSelect;
    }

    public static string LockResourceName(Guid organizationId, string targetEntityType)
    {
        return $"Field:{organizationId:N}:{targetEntityType}";
    }

    /// <summary>
    /// Describes a field for the audit trail using configuration properties only.
    /// </summary>
    public static string DescribeConfiguration(
        string name,
        bool isRequired,
        bool isActive,
        bool isPrimaryDisplayField)
    {
        return $"Name={name}; IsRequired={isRequired}; IsActive={isActive}; " +
               $"IsPrimaryDisplayField={isPrimaryDisplayField}";
    }

    public static string ObjectName => MessageCode.ObjectName.Field;
}
