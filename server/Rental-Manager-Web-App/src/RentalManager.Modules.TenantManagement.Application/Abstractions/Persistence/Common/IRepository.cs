using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common
{
    public interface IRepository<TEntity, in TPrimaryKey> where TEntity : IEntity<TPrimaryKey>
    {
        Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken = default);
        Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    }
}
