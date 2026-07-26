using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

/// <summary>
/// Makes the transaction boundary visible at the orchestration layer. Disposing
/// without a commit rolls back.
/// </summary>
internal sealed class SqlTransactionScope : ISqlTransactionScope
{
    private readonly SqlSession _session;
    private bool _completed;

    public SqlTransactionScope(SqlSession session)
    {
        _session = session;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _session.CommitTransactionAsync(cancellationToken);
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        await _session.RollbackTransactionAsync();
    }
}
