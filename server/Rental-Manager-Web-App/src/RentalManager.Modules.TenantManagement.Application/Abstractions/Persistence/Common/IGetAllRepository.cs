using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

public interface IGetAllRepository<TEntity, in TPrimaryKey>
    where TEntity : IEntity<TPrimaryKey>
    where TPrimaryKey : notnull
{
    Task<IReadOnlyCollection<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
