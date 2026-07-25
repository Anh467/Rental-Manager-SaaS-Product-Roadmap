using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Global field template catalogue in <c>[dbo]</c>. Organization-owned fields
/// live in <see cref="Org.FieldRepository"/>.
/// </summary>
public sealed class FieldRepository(
    ISqlExecutionContext executionContext,
    IOrganizationContext organizationContext)
    : BaseEntityAuditRepository<Field, Guid>(executionContext, organizationContext),
      IFieldRepository
{
}
