using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

public interface IBaseEntityAuditRepository<TEntity, in TPrimaryKey> :
    IBaseRepository<TEntity, TPrimaryKey>,
    IEntityAuditRepository<TEntity, TPrimaryKey>
    where TEntity : IEntityAudit<TPrimaryKey>
    where TPrimaryKey : notnull
{
}
