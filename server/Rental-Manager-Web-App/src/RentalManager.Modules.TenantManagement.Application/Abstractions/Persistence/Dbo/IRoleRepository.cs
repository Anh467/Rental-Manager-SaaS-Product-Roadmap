using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;

public interface IRoleRepository : IBaseEntityAuditRepository<Role, Guid>
{
    Task<Role?> FindByKeyAsync(string key, CancellationToken cancellationToken = default);
}
