using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

public interface IBaseRepository<TEntity, in TPrimaryKey> :
    IRepository<TEntity, TPrimaryKey>,
    IGetAllRepository<TEntity, TPrimaryKey>
    where TEntity : IEntity<TPrimaryKey>
    where TPrimaryKey : notnull
{
}
