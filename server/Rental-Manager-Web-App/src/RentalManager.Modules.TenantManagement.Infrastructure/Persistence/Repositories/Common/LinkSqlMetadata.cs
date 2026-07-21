using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

internal sealed class LinkSqlMetadata
{
    private LinkSqlMetadata(
        IReadOnlyList<LinkColumnMetadata> columns,
        string saveSql,
        string deleteSql)
    {
        Columns = columns;
        SaveSql = saveSql;
        DeleteSql = deleteSql;
    }

    public IReadOnlyList<LinkColumnMetadata> Columns { get; }

    public string SaveSql { get; }

    public string DeleteSql { get; }

    public static LinkSqlMetadata Create<TEntity>()
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

        LinkColumnMetadata[] columns = entityType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsMappedProperty)
            .Select(property =>
            {
                ColumnAttribute? columnAttribute =
                    property.GetCustomAttribute<ColumnAttribute>();

                string columnName = string.IsNullOrWhiteSpace(columnAttribute?.Name)
                    ? property.Name
                    : columnAttribute.Name;

                return new LinkColumnMetadata(
                    property,
                    QuoteIdentifier(columnName),
                    property.Name);
            })
            .ToArray();

        if (columns.Length == 0)
        {
            throw new InvalidOperationException(
                $"Link entity '{entityType.FullName}' has no mapped scalar properties.");
        }

        string predicate = string.Join(
            "\n  AND ",
            columns.Select(column =>
                $"({column.QuotedColumnName} = @{column.ParameterName} " +
                $"OR ({column.QuotedColumnName} IS NULL AND @{column.ParameterName} IS NULL))"));

        string columnList = string.Join(
            ", ",
            columns.Select(column => column.QuotedColumnName));

        string parameterList = string.Join(
            ", ",
            columns.Select(column => $"@{column.ParameterName}"));

        string saveSql = $"""
            IF NOT EXISTS
            (
                SELECT 1
                FROM {qualifiedTableName}
                WHERE {predicate}
            )
            BEGIN
                INSERT INTO {qualifiedTableName} ({columnList})
                VALUES ({parameterList});
            END;
            """;

        string deleteSql = $"""
            DELETE FROM {qualifiedTableName}
            WHERE {predicate};
            """;

        return new LinkSqlMetadata(columns, saveSql, deleteSql);
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

internal sealed record LinkColumnMetadata(
    PropertyInfo Property,
    string QuotedColumnName,
    string ParameterName);
