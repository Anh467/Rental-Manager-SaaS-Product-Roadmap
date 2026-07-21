using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories;

internal sealed class FieldRepository :
    BaseEntityAuditRepository<Field, Guid>,
    IFieldRepository
{
}
