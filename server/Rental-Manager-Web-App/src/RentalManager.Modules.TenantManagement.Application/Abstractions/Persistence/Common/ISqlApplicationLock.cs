namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

/// <summary>
/// Transaction-scoped mutual exclusion for business scopes that have no row to
/// lock yet, such as the first field created for a target entity type. Purely
/// technical: the caller owns the resource naming and the business rule being
/// protected.
/// </summary>
public interface ISqlApplicationLock
{
    /// <summary>
    /// Acquires an exclusive lock owned by the current transaction, so it is
    /// released automatically on commit or rollback.
    /// </summary>
    /// <param name="resourceName">
    /// Opaque lock name chosen by the caller, for example
    /// <c>Field:{organizationId}:{targetEntityType}</c>.
    /// </param>
    /// <param name="objectName">
    /// Message catalog object name used if the lock cannot be acquired.
    /// </param>
    Task AcquireAsync(
        string resourceName,
        string objectName,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
