using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;

public interface IFieldRepository :
    IBaseEntityAuditRepository<Field, Guid>
{
    Task<PagedResult<Field>> GetPagedAsync(
        bool? isActive,
        int? fieldTypeId,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<Field?> FindByKeyAsync(string key, CancellationToken cancellationToken = default);
}
