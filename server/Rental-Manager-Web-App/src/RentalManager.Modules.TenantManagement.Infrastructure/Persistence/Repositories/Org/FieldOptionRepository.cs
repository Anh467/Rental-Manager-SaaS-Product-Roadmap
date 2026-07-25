using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Org;

public sealed class FieldOptionRepository : BaseEntityAuditRepository<FieldOption, Guid>,
    IFieldOptionRepository
{
    private const string UniqueOptionKeyIndexName = "UQ_FieldOption_FieldKey";

    public FieldOptionRepository(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
        : base(executionContext, organizationContext)
    {
    }

    protected override string ObjectName => MessageCode.ObjectName.FieldOption;

    public async Task<IReadOnlyList<FieldOption>> GetByFieldIdAsync(
        Guid fieldId,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = CreateOrganizationScopedParameters();
        parameters.Add("FieldId", fieldId, DbType.Guid);

        string sql = $"""
            SELECT
                {Metadata.SelectColumnList}
            FROM {Metadata.QualifiedTableName}
            WHERE [OrganizationId] = @OrganizationId
              AND [FieldId] = @FieldId
              AND [DeletedAt] IS NULL
            ORDER BY [DisplayOrder] ASC, [Name] ASC, [Id] ASC;
            """;

        IEnumerable<FieldOption> options = await QueryAsync<FieldOption>(
            sql,
            parameters,
            cancellationToken);

        return options.ToArray();
    }

    public async Task<IReadOnlyList<FieldOption>> GetByFieldIdsAsync(
        IReadOnlyCollection<Guid> fieldIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fieldIds);

        if (fieldIds.Count == 0)
        {
            return [];
        }

        DynamicParameters parameters = CreateOrganizationScopedParameters();
        parameters.Add("FieldIds", fieldIds);

        string sql = $"""
            SELECT
                {Metadata.SelectColumnList}
            FROM {Metadata.QualifiedTableName}
            WHERE [OrganizationId] = @OrganizationId
              AND [FieldId] IN @FieldIds
              AND [DeletedAt] IS NULL
            ORDER BY [DisplayOrder] ASC, [Name] ASC, [Id] ASC;
            """;

        IEnumerable<FieldOption> options = await QueryAsync<FieldOption>(
            sql,
            parameters,
            cancellationToken);

        return options.ToArray();
    }

    public async Task RetireAsync(
        Guid optionId,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = CreateOrganizationScopedParameters();
        parameters.Add("Id", optionId, DbType.Guid);
        parameters.Add("DeletedAt", UtcNow);
        parameters.Add("UpdatedAt", UtcNow);

        string sql = $"""
            UPDATE {Metadata.QualifiedTableName}
            SET
                [DeletedAt] = @DeletedAt,
                [UpdatedAt] = @UpdatedAt
            WHERE [Id] = @Id
              AND [OrganizationId] = @OrganizationId
              AND [DeletedAt] IS NULL;
            """;

        await ExecuteAsync(sql, parameters, cancellationToken);
    }

    protected override Exception? TranslateSqlException(SqlException exception)
    {
        return SqlExceptionClassifier.IsUniqueViolation(exception, UniqueOptionKeyIndexName)
            ? new DuplicateResourceException(ObjectName, exception)
            : null;
    }
}
