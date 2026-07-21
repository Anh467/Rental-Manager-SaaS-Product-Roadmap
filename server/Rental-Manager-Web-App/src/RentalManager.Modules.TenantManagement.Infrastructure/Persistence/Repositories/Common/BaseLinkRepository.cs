using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common
{
    internal class BaseLinkRepository<TEntity> : ILinkRepository<TEntity> where TEntity : ILink
    {
        public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
