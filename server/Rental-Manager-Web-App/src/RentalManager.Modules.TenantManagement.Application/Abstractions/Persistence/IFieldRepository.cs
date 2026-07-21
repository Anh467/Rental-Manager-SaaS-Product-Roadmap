using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;

public interface IFieldRepository :
    IRepository<Field, Guid>,
    IGetAllRepository<Field, Guid>,
    IEntityAuditRepository<Field, Guid>
{
}
