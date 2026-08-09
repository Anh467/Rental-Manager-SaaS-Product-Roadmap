using Microsoft.Data.SqlClient;

namespace RentalManager.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityConnectionFactory : IIdentityConnectionFactory
{
    private readonly string _connectionString;

    public IdentityConnectionFactory(string connectionString)
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
