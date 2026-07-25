using System.Data;
using Dapper;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Org;

public sealed class RolePermissionRepository :
    BaseLinkRepository<RolePermission>,
    IRolePermissionRepository
{
    private readonly IOrganizationContext _organizationContext;

    public RolePermissionRepository(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
        : base(executionContext)
    {
        ArgumentNullException.ThrowIfNull(organizationContext);
        _organizationContext = organizationContext;
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        Guid organizationId = RequireOrganizationId();
        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", organizationId, DbType.Guid);
        parameters.Add("RoleId", roleId, DbType.Guid);

        const string sql = """
            SELECT DISTINCT permission.[Key]
            FROM [org].[RolePermission] AS rolePermission
            INNER JOIN [dbo].[Permission] AS permission
                ON permission.[Id] = rolePermission.[PermissionId]
            WHERE rolePermission.[OrganizationId] = @OrganizationId
              AND rolePermission.[RoleId] = @RoleId
              AND permission.[IsActive] = 1
            ORDER BY permission.[Key];
            """;

        IEnumerable<string> keys = await QueryAsync<string>(
            sql,
            parameters,
            cancellationToken);

        return keys.ToArray();
    }

    protected override void OnBeforeOperation(RolePermission entity)
    {
        entity.OrganizationId = RequireOrganizationId();
    }

    private Guid RequireOrganizationId()
    {
        return _organizationContext.OrganizationId
            ?? throw new MissingOrganizationContextException();
    }
}
