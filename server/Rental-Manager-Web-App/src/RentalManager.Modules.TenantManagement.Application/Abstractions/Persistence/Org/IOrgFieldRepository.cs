using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;

/// <summary>
/// Persistence for organization-owned fields. Generic get, insert, update and
/// soft delete come from the common base contracts; only queries and writes that
/// exist because of a field-specific rule are declared here.
/// </summary>
public interface IOrgFieldRepository : IBaseEntityAuditRepository<Field, Guid>
{
    Task<PagedResult<Field>> GetPagedAsync(
        string? targetEntityType,
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a field by its uniqueness scope including soft-deleted rows,
    /// because a deleted field keeps its key reserved.
    /// </summary>
    Task<Field?> FindByNormalizedKeyAsync(
        string targetEntityType,
        string normalizedKey,
        CancellationToken cancellationToken = default);

    Task<Field?> GetActivePrimaryAsync(
        string targetEntityType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the scope still has at least one active field other than
    /// <paramref name="excludedFieldId"/>.
    /// </summary>
    Task<bool> HasOtherActiveFieldsAsync(
        string targetEntityType,
        Guid excludedFieldId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the primary flag. Always run before setting a new primary so the
    /// unique filtered index is never transiently violated.
    /// </summary>
    Task ClearPrimaryAsync(
        Guid fieldId,
        CancellationToken cancellationToken = default);

    Task SetPrimaryAsync(
        Guid fieldId,
        CancellationToken cancellationToken = default);
}
