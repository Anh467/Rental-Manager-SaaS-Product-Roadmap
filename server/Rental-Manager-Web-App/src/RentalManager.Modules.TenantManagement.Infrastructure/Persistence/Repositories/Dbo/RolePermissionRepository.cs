using Dapper;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Dbo;

public sealed class RolePermissionRepository :
    BaseLinkRepository<RolePermission>,
    IRolePermissionRepository
{
    public RolePermissionRepository(ISqlExecutionContext executionContext)
        : base(executionContext)
    {
    }

    public async Task<IReadOnlySet<string>> GetPermissionKeysByRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.[Key]
            FROM [dbo].[RolePermission] rp
            INNER JOIN [dbo].[Permission] p ON p.[Id] = rp.[PermissionId]
            WHERE rp.[RoleId] = @RoleId
              AND p.[IsActive] = 1;
            """;
        var permissions = await QueryAsync<string>(
            sql,
            new DynamicParameters(new { RoleId = roleId }),
            cancellationToken);
        return permissions.ToHashSet(StringComparer.Ordinal);
    }
}
