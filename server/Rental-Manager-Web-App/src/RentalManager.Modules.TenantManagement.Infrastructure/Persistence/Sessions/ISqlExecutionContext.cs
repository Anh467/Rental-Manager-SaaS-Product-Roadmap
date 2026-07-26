using Microsoft.Data.SqlClient;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

/// <summary>
/// The connection and transaction a repository must execute on. Repositories
/// never open their own connection, so every repository in one scope shares a
/// single connection, transaction and row level security session context.
/// </summary>
public interface ISqlExecutionContext
{
    Task<SqlExecution> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The active connection plus the ambient transaction when one is open.
/// </summary>
public sealed record SqlExecution(
    SqlConnection Connection,
    SqlTransaction? Transaction);
