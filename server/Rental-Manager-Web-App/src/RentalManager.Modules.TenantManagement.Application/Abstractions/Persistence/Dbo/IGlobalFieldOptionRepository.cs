using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;

public interface IGlobalFieldOptionRepository : IBaseEntityAuditRepository<FieldOption, Guid>
{
    Task<IReadOnlyList<FieldOption>> GetByFieldIdAsync(Guid fieldId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FieldOption>> GetByFieldIdsAsync(IReadOnlyCollection<Guid> fieldIds, CancellationToken cancellationToken = default);
    Task RetireAsync(Guid optionId, CancellationToken cancellationToken = default);
}
