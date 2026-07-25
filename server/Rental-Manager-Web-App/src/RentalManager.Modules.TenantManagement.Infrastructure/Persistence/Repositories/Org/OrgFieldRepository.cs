using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Org;

/// <summary>
/// Organization-owned field persistence. Contains only the queries and writes
/// that exist because of a field rule; get, insert, update, soft delete, SQL
/// generation, connection handling and concurrency all come from the base types.
/// </summary>
public sealed class OrgFieldRepository : BaseEntityAuditRepository<Field, Guid>,
    IOrgFieldRepository
{
    private const string UniqueKeyIndexName = "UQ_Field_OrganizationTargetKey";
    private const string ActivePrimaryIndexName = "UX_Field_ActivePrimary";

    public OrgFieldRepository(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
        : base(executionContext, organizationContext)
    {
    }

    protected override string ObjectName => MessageCode.ObjectName.Field;

    public Task<PagedResult<Field>> GetPagedAsync(
        string? targetEntityType,
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parameters = new DynamicParameters();
        var predicate = new System.Text.StringBuilder();

        if (targetEntityType is not null)
        {
            predicate.Append("\n  AND [TargetEntityType] = @TargetEntityType");
            parameters.Add("TargetEntityType", targetEntityType, DbType.String, size: 64);
        }

        if (isActive is not null)
        {
            predicate.Append("\n  AND [IsActive] = @IsActive");
            parameters.Add("IsActive", isActive.Value, DbType.Boolean);
        }

        if (request.Search is not null)
        {
            predicate.Append("\n  AND ([Name] LIKE @Search OR [Key] LIKE @Search)");
            parameters.Add(
                "Search",
                $"%{EscapeLikePattern(request.Search)}%",
                DbType.String,
                size: 300);
        }

        return GetPagedAsync(
            predicate.ToString(),
            BuildOrderByClause(request),
            parameters,
            request,
            cancellationToken);
    }

    public async Task<Field?> FindByNormalizedKeyAsync(
        string targetEntityType,
        string normalizedKey,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = CreateOrganizationScopedParameters();
        parameters.Add("TargetEntityType", targetEntityType, DbType.String, size: 64);
        parameters.Add("NormalizedKey", normalizedKey, DbType.String, size: 256);

        // Soft-deleted rows are included on purpose: a deleted field keeps its
        // key reserved, matching the unfiltered unique constraint.
        string sql = $"""
            SELECT
                {Metadata.SelectColumnList}
            FROM {Metadata.QualifiedTableName}
            WHERE [OrganizationId] = @OrganizationId
              AND [TargetEntityType] = @TargetEntityType
              AND [NormalizedKey] = @NormalizedKey;
            """;

        return await QuerySingleOrDefaultAsync<Field>(
            sql,
            parameters,
            cancellationToken);
    }

    public async Task<Field?> GetActivePrimaryAsync(
        string targetEntityType,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = CreateOrganizationScopedParameters();
        parameters.Add("TargetEntityType", targetEntityType, DbType.String, size: 64);

        string sql = $"""
            SELECT
                {Metadata.SelectColumnList}
            FROM {Metadata.QualifiedTableName}
            WHERE [OrganizationId] = @OrganizationId
              AND [TargetEntityType] = @TargetEntityType
              AND [IsPrimaryDisplayField] = 1
              AND [IsActive] = 1
              AND [DeletedAt] IS NULL;
            """;

        return await QuerySingleOrDefaultAsync<Field>(
            sql,
            parameters,
            cancellationToken);
    }

    public async Task<bool> HasOtherActiveFieldsAsync(
        string targetEntityType,
        Guid excludedFieldId,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = CreateOrganizationScopedParameters();
        parameters.Add("TargetEntityType", targetEntityType, DbType.String, size: 64);
        parameters.Add("ExcludedFieldId", excludedFieldId, DbType.Guid);

        string sql = $"""
            SELECT TOP (1) 1
            FROM {Metadata.QualifiedTableName}
            WHERE [OrganizationId] = @OrganizationId
              AND [TargetEntityType] = @TargetEntityType
              AND [Id] <> @ExcludedFieldId
              AND [IsActive] = 1
              AND [DeletedAt] IS NULL;
            """;

        int? found = await ExecuteScalarAsync<int?>(
            sql,
            parameters,
            cancellationToken);

        return found is not null;
    }

    public async Task ClearPrimaryAsync(
        Guid fieldId,
        CancellationToken cancellationToken = default)
    {
        await SetPrimaryFlagAsync(
            fieldId,
            isPrimary: false,
            requireActive: false,
            cancellationToken);
    }

    public async Task SetPrimaryAsync(
        Guid fieldId,
        CancellationToken cancellationToken = default)
    {
        int affectedRows = await SetPrimaryFlagAsync(
            fieldId,
            isPrimary: true,
            requireActive: true,
            cancellationToken);

        if (affectedRows == 0)
        {
            throw new BusinessRuleException(
                MessageCode.Error.LastActivePrimaryFieldRemoval,
                new Dictionary<string, object?>
                {
                    [MessageCode.Parameter.Object] = MessageCode.ObjectName.Field
                });
        }
    }

    protected override Exception? TranslateSqlException(SqlException exception)
    {
        if (SqlExceptionClassifier.IsUniqueViolation(exception, UniqueKeyIndexName))
        {
            return new DuplicateResourceException(ObjectName, exception);
        }

        if (SqlExceptionClassifier.IsUniqueViolation(exception, ActivePrimaryIndexName))
        {
            return new BusinessRuleException(
                MessageCode.Error.MultipleActivePrimaryFields,
                new Dictionary<string, object?>
                {
                    [MessageCode.Parameter.Object] = ObjectName
                },
                exception);
        }

        return null;
    }

    private async Task<int> SetPrimaryFlagAsync(
        Guid fieldId,
        bool isPrimary,
        bool requireActive,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = CreateOrganizationScopedParameters();
        parameters.Add("Id", fieldId, DbType.Guid);
        parameters.Add("IsPrimaryDisplayField", isPrimary, DbType.Boolean);
        parameters.Add("UpdatedAt", UtcNow);

        string activePredicate = requireActive
            ? "\n  AND [IsActive] = 1"
            : string.Empty;

        // Maintaining an invariant inside an already locked transaction, so this
        // write intentionally does not take a row version from the client.
        string sql = $"""
            UPDATE {Metadata.QualifiedTableName}
            SET
                [IsPrimaryDisplayField] = @IsPrimaryDisplayField,
                [UpdatedAt] = @UpdatedAt
            WHERE [Id] = @Id
              AND [OrganizationId] = @OrganizationId
              AND [DeletedAt] IS NULL{activePredicate};
            """;

        return await WithSqlTranslationAsync(
            () => ExecuteAsync(sql, parameters, cancellationToken));
    }

    private static string BuildOrderByClause(PagedRequest request)
    {
        string sortColumn = request.SortBy?.ToLowerInvariant() switch
        {
            "name" => "[Name]",
            "key" => "[Key]",
            "createdat" => "[CreatedAt]",
            "isactive" => "[IsActive]",
            _ => "[DisplayOrder]"
        };

        string direction = request.IsDescending ? "DESC" : "ASC";

        // [Id] is the final tie-breaker so paging stays stable when the sort
        // column and the name are identical across rows.
        return $"{sortColumn} {direction}, [Name] ASC, [Id] ASC";
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
    }
}
