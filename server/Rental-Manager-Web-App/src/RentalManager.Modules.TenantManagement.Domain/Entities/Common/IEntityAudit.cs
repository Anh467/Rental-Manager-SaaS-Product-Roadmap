namespace RentalManager.Modules.TenantManagement.Domain.Entities.Common;

public interface IEntityAudit<TPrimaryKey> : IEntity<TPrimaryKey>, IAudit
    where TPrimaryKey : notnull
{
}
