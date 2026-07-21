using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

public interface IEntityAuditRepository<TEntity, in TPrimaryKey> :
    IRepository<TEntity, TPrimaryKey>,
    IGetAllRepository<TEntity, TPrimaryKey>
    where TEntity : IEntityAudit<TPrimaryKey>
    where TPrimaryKey : notnull
{
    Task SoftDeleteAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken = default);
}
