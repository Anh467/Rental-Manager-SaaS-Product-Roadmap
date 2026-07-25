using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Auditing;
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

internal sealed class FakeSqlApplicationLock : ISqlApplicationLock
{
    public List<string> AcquiredResources { get; } = [];

    public Task AcquireAsync(
        string resourceName,
        string objectName,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        AcquiredResources.Add(resourceName);
        return Task.CompletedTask;
    }
}

internal sealed class FakeAuditLogWriter : IAuditLogWriter
{
    public List<AuditLogEntry> Entries { get; } = [];

    public Task WriteAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

internal sealed class FakeOrganizationContext : IOrganizationContext
{
    public FakeOrganizationContext(Guid? organizationId, Guid? userId = null)
    {
        OrganizationId = organizationId;
        UserId = userId;
    }

    public Guid? OrganizationId { get; }

    public Guid? UserId { get; }

    public string? CorrelationId => "test-correlation";

    public bool HasOrganization => OrganizationId is not null;
}
