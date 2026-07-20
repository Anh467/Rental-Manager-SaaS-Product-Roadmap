using System.Data.Common;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Options;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Read.Connections;

internal sealed class SqlReadDbConnectionFactory : IReadDbConnectionFactory
{
    private readonly DbProviderFactory _providerFactory;
    private readonly string _connectionString;

    public SqlReadDbConnectionFactory(TenantManagementDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _providerFactory = DbProviderFactories.GetFactory(
            options.ProviderInvariantName);

        _connectionString = options.ReadConnectionString;
    }

    public async Task<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        DbConnection connection = _providerFactory.CreateConnection()
            ?? throw new InvalidOperationException(
                "The configured provider could not create a read connection.");

        connection.ConnectionString = _connectionString;

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
