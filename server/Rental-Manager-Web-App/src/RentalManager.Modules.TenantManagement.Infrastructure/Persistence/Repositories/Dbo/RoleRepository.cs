using Dapper;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Dbo;

public sealed class RoleRepository(ISqlExecutionContext executionContext, IOrganizationContext organizationContext)
    : BaseEntityAuditRepository<Role, Guid>(executionContext, organizationContext), IRoleRepository
{
    public Task<Role?> FindByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Role>($"""
            SELECT {Metadata.SelectColumnList} FROM {Metadata.QualifiedTableName}
            WHERE [Key] = @Key AND [DeletedAt] IS NULL;
            """, new DynamicParameters(new { Key = key }), cancellationToken);
}
