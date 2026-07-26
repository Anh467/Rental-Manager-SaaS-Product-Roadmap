using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

/// <summary>
/// Reflection-derived SQL for one entity. Everything is computed once per
/// closed generic type and cached in a static field on the repository.
/// </summary>
/// <remarks>
/// Organization scoping is a property of the entity, not of the connection: an
/// entity that maps <c>OrganizationId</c> gets an explicit organization
/// predicate in every statement, which supports index seeks and acts as a
/// second line of defence behind row level security.
/// </remarks>
internal sealed class EntitySqlMetadata
{
    public const string OrganizationParameterName = "OrganizationId";
    public const string ExpectedRowVersionParameterName = "ExpectedRowVersion";

    private EntitySqlMetadata(
        string qualifiedTableName,
        PropertyInfo keyProperty,
        IReadOnlyList<EntityColumnMetadata> columns,
        EntityColumnMetadata keyColumn,
        EntityColumnMetadata? organizationColumn,
        EntityColumnMetadata? rowVersionColumn,
        EntityColumnMetadata? deletedAtColumn,
        EntityColumnMetadata? isActiveColumn,
        string selectColumnList,
        string activeRowPredicate,
        string organizationPredicate,
        string selectByIdSql,
        string selectAllSql,
        string saveSql,
        string insertSql,
        string? updateSql,
        string existsSql,
        string deleteSql)
    {
        QualifiedTableName = qualifiedTableName;
        KeyProperty = keyProperty;
        Columns = columns;
        KeyColumn = keyColumn;
        OrganizationColumn = organizationColumn;
        RowVersionColumn = rowVersionColumn;
        DeletedAtColumn = deletedAtColumn;
        IsActiveColumn = isActiveColumn;
        SelectColumnList = selectColumnList;
        ActiveRowPredicate = activeRowPredicate;
        OrganizationPredicate = organizationPredicate;
        SelectByIdSql = selectByIdSql;
        SelectAllSql = selectAllSql;
        SaveSql = saveSql;
        InsertSql = insertSql;
        UpdateSql = updateSql;
        ExistsSql = existsSql;
        DeleteSql = deleteSql;
    }

    public string QualifiedTableName { get; }

    public PropertyInfo KeyProperty { get; }

    public IReadOnlyList<EntityColumnMetadata> Columns { get; }

    public EntityColumnMetadata KeyColumn { get; }

    /// <summary>Set when the entity is organization owned.</summary>
    public EntityColumnMetadata? OrganizationColumn { get; }

    /// <summary>Set when the table carries a <c>ROWVERSION</c> column.</summary>
    public EntityColumnMetadata? RowVersionColumn { get; }

    public EntityColumnMetadata? DeletedAtColumn { get; }

    public EntityColumnMetadata? IsActiveColumn { get; }

    public bool IsOrganizationOwned => OrganizationColumn is not null;

    public bool IsConcurrencyAware => RowVersionColumn is not null;

    public bool IsSoftDeletable => DeletedAtColumn is not null;

    /// <summary>
    /// Aliased column list so derived repositories can compose their own
    /// queries without rebuilding the projection.
    /// </summary>
    public string SelectColumnList { get; }

    /// <summary>
    /// <c>AND [DeletedAt] IS NULL</c>, or empty when the entity is not soft
    /// deletable.
    /// </summary>
    public string ActiveRowPredicate { get; }

    /// <summary>
    /// <c>AND [OrganizationId] = @OrganizationId</c>, or empty for global
    /// entities.
    /// </summary>
    public string OrganizationPredicate { get; }

    public string SelectByIdSql { get; }

    public string SelectAllSql { get; }

    public string SaveSql { get; }

    public string InsertSql { get; }

    /// <summary>
    /// Null when the entity has no updatable column, for example an immutable
    /// lookup table.
    /// </summary>
    public string? UpdateSql { get; }

    public string ExistsSql { get; }

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
        string qualifiedTableName =
            SqlColumnConventions.ResolveQualifiedTableName(entityType);

        PropertyInfo[] mappedProperties = entityType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(SqlColumnConventions.IsMappedProperty)
            .ToArray();

        PropertyInfo keyProperty = ResolveKeyProperty(entityType, mappedProperties);

        EntityColumnMetadata[] columns = mappedProperties
            .Select(property => CreateColumn(property, keyProperty))
            .ToArray();

        EntityColumnMetadata keyColumn = columns.Single(column => column.IsKey);
        EntityColumnMetadata? organizationColumn =
            FindColumn(columns, SqlColumnConventions.OrganizationIdPropertyName);
        EntityColumnMetadata? rowVersionColumn =
            FindColumn(columns, SqlColumnConventions.RowVersionPropertyName);
        EntityColumnMetadata? deletedAtColumn =
            FindColumn(columns, SqlColumnConventions.DeletedAtPropertyName);
        EntityColumnMetadata? isActiveColumn =
            FindColumn(columns, SqlColumnConventions.IsActivePropertyName);

        string selectColumnList = string.Join(
            ",\n    ",
            columns.Select(column =>
                $"{column.QuotedColumnName} AS " +
                SqlColumnConventions.QuoteIdentifier(column.Property.Name)));

        string activeRowPredicate = deletedAtColumn is null
            ? string.Empty
            : $"\n  AND {deletedAtColumn.QuotedColumnName} IS NULL";

        string organizationPredicate = organizationColumn is null
            ? string.Empty
            : $"\n  AND {organizationColumn.QuotedColumnName} = @{OrganizationParameterName}";

        string keyPredicate =
            $"{keyColumn.QuotedColumnName} = @{keyColumn.ParameterName}";

        string selectByIdSql = $"""
            SELECT
                {selectColumnList}
            FROM {qualifiedTableName}
            WHERE {keyPredicate}{organizationPredicate}{activeRowPredicate};
            """;

        string selectAllSql = $"""
            SELECT
                {selectColumnList}
            FROM {qualifiedTableName}
            WHERE 1 = 1{organizationPredicate}{activeRowPredicate};
            """;

        string existsSql = $"""
            SELECT TOP (1) 1
            FROM {qualifiedTableName}
            WHERE {keyPredicate}{organizationPredicate}{activeRowPredicate};
            """;

        string deleteSql = $"""
            DELETE FROM {qualifiedTableName}
            WHERE {keyPredicate}{organizationPredicate};
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

        // The row version is read back with a follow-up SELECT inside the same
        // transaction instead of an OUTPUT clause, because OUTPUT is restricted
        // on tables that carry security policy block predicates.
        string rowVersionProjection = rowVersionColumn is null
            ? "CAST(NULL AS VARBINARY(8))"
            : $"""
                (
                    SELECT {rowVersionColumn.QuotedColumnName}
                    FROM {qualifiedTableName}
                    WHERE {keyPredicate}{organizationPredicate}
                )
                """;

        string insertSql = $"""
            INSERT INTO {qualifiedTableName} ({insertColumnList})
            VALUES ({insertParameterList});

            SELECT {rowVersionProjection} AS [NewRowVersion];
            """;

        string? updateSql;
        string saveSql;

        if (updateColumns.Length == 0)
        {
            // Immutable lookup rows have nothing to assign, so the upsert
            // degrades to insert-if-absent and a plain update is unsupported.
            updateSql = null;

            saveSql = $"""
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM {qualifiedTableName}
                    WHERE {keyPredicate}{organizationPredicate}
                )
                BEGIN
                    INSERT INTO {qualifiedTableName} ({insertColumnList})
                    VALUES ({insertParameterList});
                END;
                """;
        }
        else
        {
            string updateAssignments = string.Join(
                ",\n    ",
                updateColumns.Select(column =>
                    $"{column.QuotedColumnName} = @{column.ParameterName}"));

            string concurrencyPredicate = rowVersionColumn is null
                ? string.Empty
                : $"\n  AND {rowVersionColumn.QuotedColumnName} = " +
                  $"@{ExpectedRowVersionParameterName}";

            updateSql = $"""
                UPDATE {qualifiedTableName}
                SET
                    {updateAssignments}
                WHERE {keyPredicate}{organizationPredicate}{concurrencyPredicate}{activeRowPredicate};

                DECLARE @AffectedRows INT = @@ROWCOUNT;

                SELECT
                    @AffectedRows AS [AffectedRows],
                    {rowVersionProjection} AS [NewRowVersion];
                """;

            saveSql = $"""
                UPDATE {qualifiedTableName}
                SET
                    {updateAssignments}
                WHERE {keyPredicate}{organizationPredicate};

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO {qualifiedTableName} ({insertColumnList})
                    VALUES ({insertParameterList});
                END;
                """;
        }

        return new EntitySqlMetadata(
            qualifiedTableName,
            keyProperty,
            columns,
            keyColumn,
            organizationColumn,
            rowVersionColumn,
            deletedAtColumn,
            isActiveColumn,
            selectColumnList,
            activeRowPredicate,
            organizationPredicate,
            selectByIdSql,
            selectAllSql,
            saveSql,
            insertSql,
            updateSql,
            existsSql,
            deleteSql);
    }

    private static EntityColumnMetadata? FindColumn(
        IReadOnlyCollection<EntityColumnMetadata> columns,
        string propertyName)
    {
        return columns.FirstOrDefault(
            column => string.Equals(
                column.Property.Name,
                propertyName,
                StringComparison.Ordinal));
    }

    private static EntityColumnMetadata CreateColumn(
        PropertyInfo property,
        PropertyInfo keyProperty)
    {
        string columnName = SqlColumnConventions.ResolveColumnName(property);

        bool isKey = property == keyProperty;
        bool isGenerated = SqlColumnConventions.IsDatabaseGenerated(property);

        bool isInsertable = !isGenerated;

        // CreatedAt is write-once, and OrganizationId is assigned from the
        // trusted context on insert so no update path can move a row between
        // organizations.
        bool isUpdatable =
            !isKey &&
            !isGenerated &&
            !string.Equals(
                property.Name,
                SqlColumnConventions.CreatedAtPropertyName,
                StringComparison.Ordinal) &&
            !string.Equals(
                property.Name,
                SqlColumnConventions.OrganizationIdPropertyName,
                StringComparison.Ordinal);

        return new EntityColumnMetadata(
            property,
            columnName,
            SqlColumnConventions.QuoteIdentifier(columnName),
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
                string.Equals(
                    property.Name,
                    SqlColumnConventions.DefaultKeyPropertyName,
                    StringComparison.Ordinal));

        return keyProperty
            ?? throw new InvalidOperationException(
                $"Entity '{entityType.FullName}' must expose an 'Id' property " +
                "or mark one mapped property with [Key].");
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

/// <summary>
/// Outcome of a concurrency-aware update: how many rows matched every predicate
/// and, when the table has one, the row version the row now carries.
/// </summary>
internal sealed class EntityUpdateResult
{
    public int AffectedRows { get; init; }

    public byte[]? NewRowVersion { get; init; }
}
