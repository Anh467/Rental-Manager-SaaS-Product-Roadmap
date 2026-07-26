namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

/// <summary>
/// The single organization-aware database session for one unit of work. Every
/// repository resolved from the same scope shares its connection, its
/// transaction and its row level security session context.
/// </summary>
public interface ISqlSession
{
    /// <summary>
    /// Opens the transaction boundary for a compound write. Simple reads do not
    /// need this; they run on the same session without an explicit transaction.
    /// </summary>
    Task<ISqlTransactionScope> BeginTransactionAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// An open transaction. Disposing without committing rolls back.
/// </summary>
public interface ISqlTransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
