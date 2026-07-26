using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

public interface IRepository<TEntity, in TPrimaryKey>
    where TEntity : IEntity<TPrimaryKey>
    where TPrimaryKey : notnull
{
    /// <summary>
    /// Returns the active row, or <see langword="null"/> when it does not exist
    /// or belongs to another organization. Callers decide how to surface that.
    /// </summary>
    Task<TEntity?> GetAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new row and returns the generated row version when the table
    /// has one.
    /// </summary>
    Task<byte[]?> InsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing row. When the table has a row version,
    /// <paramref name="expectedRowVersion"/> is required and a mismatch raises
    /// a concurrency conflict rather than silently inserting a new row.
    /// </summary>
    Task<byte[]?> UpdateAsync(
        TEntity entity,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}
