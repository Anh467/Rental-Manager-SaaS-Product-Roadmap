using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Dbo;

public sealed class FieldTypeRepository(
    ISqlExecutionContext executionContext,
    IOrganizationContext organizationContext)
    : BaseRepository<FieldType, int>(executionContext, organizationContext),
      IFieldTypeRepository
{
}
