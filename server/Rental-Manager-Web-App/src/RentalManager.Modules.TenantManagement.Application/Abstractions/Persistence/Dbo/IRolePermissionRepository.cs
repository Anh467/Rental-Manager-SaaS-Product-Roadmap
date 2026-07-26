using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;

public interface IRolePermissionRepository : ILinkRepository<RolePermission>
{
    Task<IReadOnlySet<string>> GetPermissionKeysByRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}
