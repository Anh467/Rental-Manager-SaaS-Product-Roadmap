using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;

public interface IFieldTypeRepository : IBaseRepository<FieldType, int>
{
}
