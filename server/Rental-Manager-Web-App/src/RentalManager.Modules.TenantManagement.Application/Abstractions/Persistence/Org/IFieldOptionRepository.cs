using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;

public interface IFieldOptionRepository : IBaseEntityAuditRepository<FieldOption, Guid>
{
    Task<IReadOnlyList<FieldOption>> GetByFieldIdAsync(
        Guid fieldId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FieldOption>> GetByFieldIdsAsync(
        IReadOnlyCollection<Guid> fieldIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes the option, bypassing the generic row version check because
    /// the caller already holds the parent field under an exclusive lock inside
    /// the same transaction.
    /// </summary>
    Task RetireAsync(
        Guid optionId,
        CancellationToken cancellationToken = default);
}
