using System.Data;
using System.Data.Common;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Options;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Write.Connections;

internal sealed class SqlWriteDbSession : IWriteDbSession
{
    private readonly DbConnection _connection;
    private bool _disposed;

    public SqlWriteDbSession(TenantManagementDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        DbProviderFactory providerFactory =
            DbProviderFactories.GetFactory(options.ProviderInvariantName);

        _connection = providerFactory.CreateConnection()
            ?? throw new InvalidOperationException(
                $"Provider '{options.ProviderInvariantName}' could not create a connection.");

        _connection.ConnectionString = options.WriteConnectionString;
    }

    public DbConnection Connection => _connection;

    public DbTransaction? Transaction { get; private set; }

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        Transaction ??= await _connection.BeginTransactionAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (Transaction is not null)
        {
            await Transaction.DisposeAsync();
            Transaction = null;
        }

        await _connection.DisposeAsync();
        _disposed = true;
    }
}
