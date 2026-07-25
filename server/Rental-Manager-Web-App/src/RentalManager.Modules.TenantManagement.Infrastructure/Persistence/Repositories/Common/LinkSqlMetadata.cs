using System.Reflection;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

internal sealed class LinkSqlMetadata
{
    private LinkSqlMetadata(
        IReadOnlyList<LinkColumnMetadata> columns,
        string existsSql,
        string saveSql,
        string deleteSql)
    {
        Columns = columns;
        ExistsSql = existsSql;
        SaveSql = saveSql;
        DeleteSql = deleteSql;
    }

    public IReadOnlyList<LinkColumnMetadata> Columns { get; }

    public string ExistsSql { get; }

    public string SaveSql { get; }

    public string DeleteSql { get; }

    public static LinkSqlMetadata Create<TEntity>()
    {
        Type entityType = typeof(TEntity);
        string qualifiedTableName =
            SqlColumnConventions.ResolveQualifiedTableName(entityType);

        LinkColumnMetadata[] columns = entityType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(SqlColumnConventions.IsMappedProperty)
            .Select(property =>
            {
                string columnName = SqlColumnConventions.ResolveColumnName(property);

                return new LinkColumnMetadata(
                    property,
                    SqlColumnConventions.QuoteIdentifier(columnName),
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

        string existsSql = $"""
            SELECT TOP (1) 1
            FROM {qualifiedTableName}
            WHERE {predicate};
            """;

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

        return new LinkSqlMetadata(columns, existsSql, saveSql, deleteSql);
    }
}

internal sealed record LinkColumnMetadata(
    PropertyInfo Property,
    string QuotedColumnName,
    string ParameterName);
