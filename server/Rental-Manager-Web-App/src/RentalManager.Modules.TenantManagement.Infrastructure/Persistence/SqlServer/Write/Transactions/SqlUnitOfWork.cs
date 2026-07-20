using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Write.Transactions;

internal sealed class SqlUnitOfWork : IUnitOfWork
{
    private readonly IWriteDbSession _session;

    public SqlUnitOfWork(IWriteDbSession session)
    {
        _session = session;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _session.OpenAsync(cancellationToken);

        if (_session.Transaction is null)
        {
            throw new InvalidOperationException("No active write transaction exists.");
        }

        await _session.Transaction.CommitAsync(cancellationToken);
    }
}
