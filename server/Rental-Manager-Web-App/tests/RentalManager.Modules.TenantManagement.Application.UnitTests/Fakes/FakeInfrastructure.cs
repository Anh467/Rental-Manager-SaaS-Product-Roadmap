using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests.Fakes;

internal sealed class FakeSqlSession : ISqlSession
{
    public int BeganTransactions { get; private set; }

    public int CommittedTransactions { get; private set; }

    public int RolledBackTransactions { get; private set; }

    public Task<ISqlTransactionScope> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        BeganTransactions++;
        return Task.FromResult<ISqlTransactionScope>(new Scope(this));
    }

    private sealed class Scope : ISqlTransactionScope
    {
        private readonly FakeSqlSession _session;
        private bool _committed;

        public Scope(FakeSqlSession session)
        {
            _session = session;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            _committed = true;
            _session.CommittedTransactions++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_committed)
            {
                _session.RolledBackTransactions++;
            }

            return ValueTask.CompletedTask;
        }
    }
}
