using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;

public interface IRolePermissionRepository : ILinkRepository<RolePermission>
{
    Task<IReadOnlyCollection<string>> GetPermissionKeysByRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}
