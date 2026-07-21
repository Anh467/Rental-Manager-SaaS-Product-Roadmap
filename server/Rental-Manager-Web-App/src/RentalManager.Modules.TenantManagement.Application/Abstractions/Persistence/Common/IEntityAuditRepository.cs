using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common
{
    public interface IEntityAuditRepository<TEntity, in TPrimaryKey> where TEntity : IEntityAudit<TPrimaryKey>
    {
        Task SoftDeleteAsync(TPrimaryKey id, CancellationToken cancellationToken = default);
    }
}
