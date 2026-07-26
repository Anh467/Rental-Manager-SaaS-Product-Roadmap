using Microsoft.Data.SqlClient;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Connections;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

/// <summary>
/// Scoped owner of the database connection, the transaction boundary and the
/// row level security session context for one unit of work.
/// </summary>
public sealed class SqlSession : ISqlSession, ISqlExecutionContext, IAsyncDisposable
{
    private const string OrganizationSessionKey = "OrganizationId";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOrganizationContext _organizationContext;

    private SqlConnection? _connection;
    private SqlTransaction? _transaction;
    private Guid? _appliedOrganizationId;
    private bool _hasAppliedOrganizationId;
    private bool _disposed;

    public SqlSession(
        ISqlConnectionFactory connectionFactory,
        IOrganizationContext organizationContext)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(organizationContext);

        _connectionFactory = connectionFactory;
        _organizationContext = organizationContext;
    }

    public async Task<SqlExecution> GetAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _connection ??= await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await ApplyOrganizationSessionContextAsync(cancellationToken);

        return new SqlExecution(_connection, _transaction);
    }

    public async Task<ISqlTransactionScope> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already open on this session. Nested " +
                "transactions are not supported; pass the existing scope down.");
        }

        SqlExecution execution = await GetAsync(cancellationToken);

        _transaction = (SqlTransaction)await execution.Connection
            .BeginTransactionAsync(cancellationToken);

        return new SqlTransactionScope(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await RollbackTransactionAsync();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    internal async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "There is no open transaction to commit.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    internal async ValueTask RollbackTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        SqlTransaction transaction = _transaction;
        _transaction = null;

        try
        {
            // The connection can already be broken, in which case the server
            // has rolled the transaction back for us.
            if (transaction.Connection is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
        }
        catch (SqlException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    /// <summary>
    /// Publishes the organization to <c>SESSION_CONTEXT</c> so row level
    /// security can filter. Applied on every state change rather than only on
    /// open, so a pooled connection can never keep another organization's
    /// context, and a null organization explicitly clears it so unscoped access
    /// to <c>[org]</c> data fails closed.
    /// </summary>
    private async Task ApplyOrganizationSessionContextAsync(
        CancellationToken cancellationToken)
    {
        Guid? organizationId = _organizationContext.OrganizationId;

        if (_hasAppliedOrganizationId && _appliedOrganizationId == organizationId)
        {
            return;
        }

        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "The organization context must not change while a transaction " +
                "is open.");
        }

        const string sql =
            "EXEC sp_set_session_context @key = N'" +
            OrganizationSessionKey +
            "', @value = @OrganizationId;";

        await using SqlCommand command = new(sql, _connection);

        SqlParameter parameter = command.Parameters.Add(
            "@OrganizationId",
            System.Data.SqlDbType.UniqueIdentifier);

        parameter.Value = organizationId.HasValue
            ? organizationId.Value
            : DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken);

        _appliedOrganizationId = organizationId;
        _hasAppliedOrganizationId = true;
    }
}
