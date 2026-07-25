using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

/// <summary>
/// Identifier quoting and property-to-column conventions shared by every SQL
/// metadata builder.
/// </summary>
internal static class SqlColumnConventions
{
    public const string DefaultSchema = "dbo";

    public const string OrganizationIdPropertyName = "OrganizationId";
    public const string RowVersionPropertyName = "RowVersion";
    public const string DeletedAtPropertyName = "DeletedAt";
    public const string IsActivePropertyName = "IsActive";
    public const string CreatedAtPropertyName = "CreatedAt";
    public const string UpdatedAtPropertyName = "UpdatedAt";
    public const string DefaultKeyPropertyName = "Id";

    public static string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    public static string ResolveQualifiedTableName(Type entityType)
    {
        TableAttribute? tableAttribute = entityType.GetCustomAttribute<TableAttribute>();

        string tableName = string.IsNullOrWhiteSpace(tableAttribute?.Name)
            ? entityType.Name
            : tableAttribute.Name;

        string schemaName = string.IsNullOrWhiteSpace(tableAttribute?.Schema)
            ? DefaultSchema
            : tableAttribute.Schema;

        return $"{QuoteIdentifier(schemaName)}.{QuoteIdentifier(tableName)}";
    }

    public static string ResolveColumnName(PropertyInfo property)
    {
        ColumnAttribute? columnAttribute = property.GetCustomAttribute<ColumnAttribute>();

        return string.IsNullOrWhiteSpace(columnAttribute?.Name)
            ? property.Name
            : columnAttribute.Name;
    }

    public static bool IsMappedProperty(PropertyInfo property)
    {
        return property.CanRead &&
               property.CanWrite &&
               property.GetIndexParameters().Length == 0 &&
               property.GetCustomAttribute<NotMappedAttribute>() is null &&
               IsSupportedColumnType(property.PropertyType);
    }

    public static bool IsSupportedColumnType(Type propertyType)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        return type.IsEnum ||
               type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(Guid) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(DateOnly) ||
               type == typeof(TimeOnly) ||
               type == typeof(byte[]);
    }

    public static bool IsDatabaseGenerated(PropertyInfo property)
    {
        DatabaseGeneratedAttribute? generatedAttribute =
            property.GetCustomAttribute<DatabaseGeneratedAttribute>();

        return generatedAttribute?.DatabaseGeneratedOption is
            DatabaseGeneratedOption.Identity or DatabaseGeneratedOption.Computed;
    }
}
