using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Dbo;

public sealed class GlobalFieldOptionRepository(
    ISqlExecutionContext executionContext,
    IOrganizationContext organizationContext)
    : BaseEntityAuditRepository<FieldOption, Guid>(executionContext, organizationContext),
      IGlobalFieldOptionRepository
{
    protected override string ObjectName => MessageCode.ObjectName.FieldOption;

    public async Task<IReadOnlyList<FieldOption>> GetByFieldIdAsync(Guid fieldId, CancellationToken cancellationToken = default) =>
        await GetByFieldIdsAsync([fieldId], cancellationToken);

    public async Task<IReadOnlyList<FieldOption>> GetByFieldIdsAsync(IReadOnlyCollection<Guid> fieldIds, CancellationToken cancellationToken = default)
    {
        if (fieldIds.Count == 0) return [];
        var parameters = new DynamicParameters();
        parameters.Add("FieldIds", fieldIds);
        return (await QueryAsync<FieldOption>($"""
            SELECT {Metadata.SelectColumnList} FROM {Metadata.QualifiedTableName}
            WHERE [FieldId] IN @FieldIds AND [DeletedAt] IS NULL
            ORDER BY [DisplayOrder], [Name], [Id];
            """, parameters, cancellationToken)).ToArray();
    }

    public async Task RetireAsync(Guid optionId, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", optionId, DbType.Guid);
        parameters.Add("UpdatedAt", UtcNow);
        await ExecuteAsync($"""
            UPDATE {Metadata.QualifiedTableName}
            SET [DeletedAt] = @UpdatedAt, [UpdatedAt] = @UpdatedAt
            WHERE [Id] = @Id AND [DeletedAt] IS NULL;
            """, parameters, cancellationToken);
    }

    protected override Exception? TranslateSqlException(SqlException exception) =>
        SqlExceptionClassifier.IsUniqueViolation(exception, "UQ_FieldOption_FieldKey")
            ? new DuplicateResourceException(ObjectName, "options", exception) : null;
}
