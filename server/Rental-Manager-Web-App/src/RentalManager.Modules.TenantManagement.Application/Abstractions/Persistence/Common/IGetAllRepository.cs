using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace RentalManager.Modules.Identity.Application.Abstractions.Persistence.Common
{
    public interface IGetAllRepository<TEntity, in TPrimaryKey> where TEntity : IEntity<TPrimaryKey>
    {
        Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
