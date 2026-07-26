using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;

/// <summary>
/// Organization field catalogue persistence. Generic CRUD, soft delete, and
/// row-version handling come from the common repository contracts.
/// </summary>
public interface IOrgFieldRepository : IBaseEntityAuditRepository<Field, Guid>
{
    Task<PagedResult<Field>> GetPagedAsync(
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Includes soft-deleted rows because a deleted field keeps its key reserved.
    /// </summary>
    Task<Field?> FindByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);
}
