using System.Data;
using Dapper;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Common;

/// <summary>
/// <c>sp_getapplock</c> with the transaction as the lock owner, so the lock is
/// always released by commit or rollback and can never leak.
/// </summary>
public sealed class SqlApplicationLock : ISqlApplicationLock
{
    private const int GrantedImmediately = 0;
    private const int GrantedAfterWait = 1;
    private const int TimedOut = -1;
    private const int Cancelled = -2;
    private const int DeadlockVictim = -3;
    private const int InvalidCall = -999;

    private readonly ISqlExecutionContext _executionContext;

    public SqlApplicationLock(ISqlExecutionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        _executionContext = executionContext;
    }

    public async Task AcquireAsync(
        string resourceName,
        string objectName,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        if (execution.Transaction is null)
        {
            throw new InvalidOperationException(
                "An application lock owned by the transaction requires an open " +
                "transaction. Begin one before acquiring the lock.");
        }

        var parameters = new DynamicParameters();
        parameters.Add("Resource", resourceName, DbType.String, size: 255);
        parameters.Add("LockMode", "Exclusive", DbType.String, size: 32);
        parameters.Add("LockOwner", "Transaction", DbType.String, size: 32);
        parameters.Add(
            "LockTimeout",
            (int)Math.Clamp(timeout.TotalMilliseconds, 0, int.MaxValue),
            DbType.Int32);
        parameters.Add(
            "Result",
            dbType: DbType.Int32,
            direction: ParameterDirection.ReturnValue);

        await execution.Connection.ExecuteAsync(
            new CommandDefinition(
                "sp_getapplock",
                parameters,
                execution.Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        int result = parameters.Get<int>("Result");

        switch (result)
        {
            case GrantedImmediately:
            case GrantedAfterWait:
                return;

            case TimedOut:
            case DeadlockVictim:
                throw new ConcurrencyConflictException(objectName);

            case Cancelled:
                throw new OperationCanceledException(
                    $"Acquiring application lock '{resourceName}' was cancelled.");

            case InvalidCall:
                throw new InvalidOperationException(
                    $"Application lock '{resourceName}' was requested with " +
                    "invalid arguments.");

            default:
                throw new InvalidOperationException(
                    $"sp_getapplock returned unexpected code {result} for " +
                    $"resource '{resourceName}'.");
        }
    }
}
