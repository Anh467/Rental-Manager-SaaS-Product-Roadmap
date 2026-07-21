using Microsoft.Data.SqlClient;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Connections;

internal interface ISqlConnectionFactory
{
    Task<SqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default);
}

internal sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<SqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);

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
