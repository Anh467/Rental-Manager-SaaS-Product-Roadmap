using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

public interface IRepository<TEntity, in TPrimaryKey>
    where TEntity : IEntity<TPrimaryKey>
    where TPrimaryKey : notnull
{
    Task<TEntity> GetAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}
