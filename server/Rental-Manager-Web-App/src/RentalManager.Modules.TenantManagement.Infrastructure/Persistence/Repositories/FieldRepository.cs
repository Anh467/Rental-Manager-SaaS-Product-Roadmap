using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Global field template catalogue in <c>[dbo]</c>. Organization-owned fields
/// live in <see cref="Org.FieldRepository"/>.
/// </summary>
public sealed class FieldRepository(
    ISqlExecutionContext executionContext,
    IOrganizationContext organizationContext)
    : BaseEntityAuditRepository<Field, Guid>(executionContext, organizationContext),
      IFieldRepository
{
    private const string UniqueKeyConstraintName = "UQ_Field_Key";

    protected override string ObjectName => MessageCode.ObjectName.Field;

    public Task<PagedResult<Field>> GetPagedAsync(
        bool? isActive, int? fieldTypeId, PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        var predicate = new System.Text.StringBuilder();
        if (isActive is not null)
        {
            predicate.Append("\n AND [IsActive] = @IsActive");
            parameters.Add("IsActive", isActive.Value, DbType.Boolean);
        }
        if (fieldTypeId is not null)
        {
            predicate.Append("\n AND [FieldTypeId] = @FieldTypeId");
            parameters.Add("FieldTypeId", fieldTypeId.Value, DbType.Int32);
        }
        if (request.Search is not null)
        {
            predicate.Append("\n AND ([Name] LIKE @Search OR [Key] LIKE @Search)");
            parameters.Add("Search", $"%{request.Search.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]")}%");
        }
        string column = request.SortBy?.ToLowerInvariant() switch
        {
            "key" => "[Key]",
            "createdat" => "[CreatedAt]",
            "updatedat" => "[UpdatedAt]",
            "isactive" => "[IsActive]",
            "fieldtype" or "fieldtypeid" => "[FieldTypeId]",
            _ => "[Name]"
        };
        return GetPagedAsync(predicate.ToString(),
            $"{column} {(request.IsDescending ? "DESC" : "ASC")}, [Id] ASC",
            parameters, request, cancellationToken);
    }

    public async Task<Field?> FindByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Key", key, DbType.String, size: DefinitionConstants.InlineTextMaxLength);
        return await QuerySingleOrDefaultAsync<Field>($"""
            SELECT {Metadata.SelectColumnList} FROM {Metadata.QualifiedTableName}
            WHERE [Key] = @Key;
            """, parameters, cancellationToken);
    }

    protected override Exception? TranslateSqlException(SqlException exception) =>
        SqlExceptionClassifier.IsUniqueViolation(exception, UniqueKeyConstraintName)
            ? new DuplicateResourceException(ObjectName, "key", exception)
            : null;
}
