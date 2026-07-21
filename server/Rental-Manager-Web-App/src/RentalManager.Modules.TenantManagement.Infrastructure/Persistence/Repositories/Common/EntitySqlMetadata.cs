using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

internal sealed class EntitySqlMetadata
{
    private EntitySqlMetadata(
        string qualifiedTableName,
        PropertyInfo keyProperty,
        IReadOnlyList<EntityColumnMetadata> columns,
        string selectByIdSql,
        string selectAllSql,
        string saveSql,
        string deleteSql)
    {
        QualifiedTableName = qualifiedTableName;
        KeyProperty = keyProperty;
        Columns = columns;
        SelectByIdSql = selectByIdSql;
        SelectAllSql = selectAllSql;
        SaveSql = saveSql;
        DeleteSql = deleteSql;
    }

    public string QualifiedTableName { get; }

    public PropertyInfo KeyProperty { get; }

    public IReadOnlyList<EntityColumnMetadata> Columns { get; }

    public string SelectByIdSql { get; }

    public string SelectAllSql { get; }

    public string SaveSql { get; }

    public string DeleteSql { get; }

    public EntityColumnMetadata? FindColumn(string propertyName)
    {
        return Columns.FirstOrDefault(
            column => string.Equals(
                column.Property.Name,
                propertyName,
                StringComparison.Ordinal));
    }

    public static EntitySqlMetadata Create<TEntity>()
    {
        Type entityType = typeof(TEntity);
        TableAttribute? tableAttribute = entityType.GetCustomAttribute<TableAttribute>();

        string tableName = string.IsNullOrWhiteSpace(tableAttribute?.Name)
            ? entityType.Name
            : tableAttribute.Name;

        string schemaName = string.IsNullOrWhiteSpace(tableAttribute?.Schema)
            ? "dbo"
            : tableAttribute.Schema;

        string qualifiedTableName =
            $"{QuoteIdentifier(schemaName)}.{QuoteIdentifier(tableName)}";

        PropertyInfo[] mappedProperties = entityType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsMappedProperty)
            .ToArray();

        PropertyInfo keyProperty = ResolveKeyProperty(entityType, mappedProperties);

        EntityColumnMetadata[] columns = mappedProperties
            .Select(property => CreateColumn(property, keyProperty))
            .ToArray();

        EntityColumnMetadata keyColumn = columns.Single(column => column.IsKey);
        EntityColumnMetadata? deletedAtColumn = columns.FirstOrDefault(
            column => string.Equals(
                column.Property.Name,
                "DeletedAt",
                StringComparison.Ordinal));

        string selectColumns = string.Join(
            ",\n    ",
            columns.Select(column =>
                $"{column.QuotedColumnName} AS {QuoteIdentifier(column.Property.Name)}"));

        string activeRowPredicate = deletedAtColumn is null
            ? string.Empty
            : $"\n  AND {deletedAtColumn.QuotedColumnName} IS NULL";

        string selectByIdSql = $"""
            SELECT
                {selectColumns}
            FROM {qualifiedTableName}
            WHERE {keyColumn.QuotedColumnName} = @{keyColumn.ParameterName}{activeRowPredicate};
            """;

        string selectAllPredicate = deletedAtColumn is null
            ? string.Empty
            : $"\nWHERE {deletedAtColumn.QuotedColumnName} IS NULL";

        string selectAllSql = $"""
            SELECT
                {selectColumns}
            FROM {qualifiedTableName}{selectAllPredicate};
            """;

        EntityColumnMetadata[] insertColumns = columns
            .Where(column => column.IsInsertable)
            .ToArray();

        EntityColumnMetadata[] updateColumns = columns
            .Where(column => column.IsUpdatable)
            .ToArray();

        string insertColumnList = string.Join(
            ", ",
            insertColumns.Select(column => column.QuotedColumnName));

        string insertParameterList = string.Join(
            ", ",
            insertColumns.Select(column => $"@{column.ParameterName}"));

        string insertSql = $"""
            INSERT INTO {qualifiedTableName} ({insertColumnList})
            VALUES ({insertParameterList});
            """;

        string saveSql;

        if (updateColumns.Length == 0)
        {
            saveSql = $"""
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM {qualifiedTableName}
                    WHERE {keyColumn.QuotedColumnName} = @{keyColumn.ParameterName}
                )
                BEGIN
                    {insertSql}
                END;
                """;
        }
        else
        {
            string updateAssignments = string.Join(
                ",\n    ",
                updateColumns.Select(column =>
                    $"{column.QuotedColumnName} = @{column.ParameterName}"));

            saveSql = $"""
                UPDATE {qualifiedTableName}
                SET
                    {updateAssignments}
                WHERE {keyColumn.QuotedColumnName} = @{keyColumn.ParameterName};

                IF @@ROWCOUNT = 0
                BEGIN
                    {insertSql}
                END;
                """;
        }

        string deleteSql = $"""
            DELETE FROM {qualifiedTableName}
            WHERE {keyColumn.QuotedColumnName} = @{keyColumn.ParameterName};
            """;

        return new EntitySqlMetadata(
            qualifiedTableName,
            keyProperty,
            columns,
            selectByIdSql,
            selectAllSql,
            saveSql,
            deleteSql);
    }

    private static EntityColumnMetadata CreateColumn(
        PropertyInfo property,
        PropertyInfo keyProperty)
    {
        ColumnAttribute? columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        DatabaseGeneratedAttribute? generatedAttribute =
            property.GetCustomAttribute<DatabaseGeneratedAttribute>();

        string columnName = string.IsNullOrWhiteSpace(columnAttribute?.Name)
            ? property.Name
            : columnAttribute.Name;

        bool isKey = property == keyProperty;
        bool isGenerated = generatedAttribute?.DatabaseGeneratedOption is
            DatabaseGeneratedOption.Identity or DatabaseGeneratedOption.Computed;

        bool isInsertable = !isGenerated;
        bool isUpdatable =
            !isKey &&
            !isGenerated &&
            !string.Equals(property.Name, "CreatedAt", StringComparison.Ordinal);

        return new EntityColumnMetadata(
            property,
            columnName,
            QuoteIdentifier(columnName),
            property.Name,
            isKey,
            isInsertable,
            isUpdatable);
    }

    private static PropertyInfo ResolveKeyProperty(
        Type entityType,
        IReadOnlyCollection<PropertyInfo> mappedProperties)
    {
        PropertyInfo[] explicitKeys = mappedProperties
            .Where(property => property.GetCustomAttribute<KeyAttribute>() is not null)
            .ToArray();

        if (explicitKeys.Length > 1)
        {
            throw new InvalidOperationException(
                $"Entity '{entityType.FullName}' has more than one [Key] property. " +
                "The common repository supports one primary key only.");
        }

        PropertyInfo? keyProperty = explicitKeys.SingleOrDefault()
            ?? mappedProperties.FirstOrDefault(property =>
                string.Equals(property.Name, "Id", StringComparison.Ordinal));

        return keyProperty
            ?? throw new InvalidOperationException(
                $"Entity '{entityType.FullName}' must expose an 'Id' property " +
                "or mark one mapped property with [Key].");
    }

    private static bool IsMappedProperty(PropertyInfo property)
    {
        return property.CanRead &&
               property.CanWrite &&
               property.GetIndexParameters().Length == 0 &&
               property.GetCustomAttribute<NotMappedAttribute>() is null &&
               IsSupportedColumnType(property.PropertyType);
    }

    private static bool IsSupportedColumnType(Type propertyType)
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

    private static string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}

internal sealed record EntityColumnMetadata(
    PropertyInfo Property,
    string ColumnName,
    string QuotedColumnName,
    string ParameterName,
    bool IsKey,
    bool IsInsertable,
    bool IsUpdatable);
