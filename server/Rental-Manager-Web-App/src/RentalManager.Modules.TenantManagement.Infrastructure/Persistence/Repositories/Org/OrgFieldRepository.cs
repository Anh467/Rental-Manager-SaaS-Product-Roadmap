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

public sealed class OrgFieldRepository : BaseEntityAuditRepository<Field, Guid>,
    IOrgFieldRepository
{
    private const string UniqueKeyConstraintName = "UQ_Field_OrganizationKey";

    public OrgFieldRepository(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
        : base(executionContext, organizationContext)
    {
    }

    protected override string ObjectName => MessageCode.ObjectName.Field;

    public Task<PagedResult<Field>> GetPagedAsync(
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parameters = new DynamicParameters();
        var predicate = new System.Text.StringBuilder();

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

    public async Task<Field?> FindByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = CreateOrganizationScopedParameters();
        parameters.Add(
            "Key",
            key,
            DbType.String,
            size: DefinitionConstants.InlineTextMaxLength);

        string sql = $"""
            SELECT
                {Metadata.SelectColumnList}
            FROM {Metadata.QualifiedTableName}
            WHERE [OrganizationId] = @OrganizationId
              AND [Key] = @Key;
            """;

        return await QuerySingleOrDefaultAsync<Field>(
            sql,
            parameters,
            cancellationToken);
    }

    protected override Exception? TranslateSqlException(SqlException exception)
    {
        return SqlExceptionClassifier.IsUniqueViolation(
            exception,
            UniqueKeyConstraintName)
            ? new DuplicateResourceException(ObjectName, "key", exception)
            : null;
    }

    private static string BuildOrderByClause(PagedRequest request)
    {
        string sortColumn = request.SortBy?.ToLowerInvariant() switch
        {
            "name" => "[Name]",
            "key" => "[Key]",
            "createdat" => "[CreatedAt]",
            "isactive" => "[IsActive]",
            _ => "[Name]"
        };

        string direction = request.IsDescending ? "DESC" : "ASC";
        return $"{sortColumn} {direction}, [Id] ASC";
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
    }
}
