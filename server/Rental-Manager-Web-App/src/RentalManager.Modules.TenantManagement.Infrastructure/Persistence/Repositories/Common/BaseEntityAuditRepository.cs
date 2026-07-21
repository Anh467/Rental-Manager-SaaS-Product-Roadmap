using RentalManager.Modules.Identity.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common
{
    public class BaseEntityAuditRepository<TEntity, TPrimaryKey> : 
        IEntityAuditRepository<TEntity, TPrimaryKey>, 
        IGetAllRepository<TEntity, TPrimaryKey>,
        IRepository<TEntity, TPrimaryKey>
        where TEntity : class, IEntityAudit<TPrimaryKey>
    {
        public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SoftDeleteAsync(TPrimaryKey id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
