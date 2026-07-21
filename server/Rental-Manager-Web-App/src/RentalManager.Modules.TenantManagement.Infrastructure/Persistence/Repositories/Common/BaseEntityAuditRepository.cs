using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

internal class BaseEntityAuditRepository<TEntity, TPrimaryKey> :
    BaseRepository<TEntity, TPrimaryKey>,
    IEntityAuditRepository<TEntity, TPrimaryKey>
    where TEntity : class, IEntityAudit<TPrimaryKey>
    where TPrimaryKey : notnull
{
    public Task SoftDeleteAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
