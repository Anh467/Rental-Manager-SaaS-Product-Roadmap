using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

public interface IEntityAuditRepository<TEntity, in TPrimaryKey>
    where TEntity : IEntityAudit<TPrimaryKey>
    where TPrimaryKey : notnull
{
    /// <summary>
    /// Marks the row deleted. When the table has a row version,
    /// <paramref name="expectedRowVersion"/> is required.
    /// </summary>
    Task SoftDeleteAsync(
        TPrimaryKey id,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default);
}
