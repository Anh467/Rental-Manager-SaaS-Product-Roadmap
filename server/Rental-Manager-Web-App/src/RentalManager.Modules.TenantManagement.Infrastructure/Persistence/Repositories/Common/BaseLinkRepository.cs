using Dapper;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

/// <summary>
/// Persistence for join tables that have no surrogate key. Shares the ambient
/// session so link writes participate in the caller's transaction.
/// </summary>
public abstract class BaseLinkRepository<TEntity> : ILinkRepository<TEntity>
    where TEntity : class, ILink
{
    private static readonly LinkSqlMetadata Metadata =
        LinkSqlMetadata.Create<TEntity>();

    private readonly ISqlExecutionContext _executionContext;

    protected BaseLinkRepository(ISqlExecutionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        _executionContext = executionContext;
    }

    public async Task<bool> ExistsAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        OnBeforeOperation(entity);

        DynamicParameters parameters = CreateParameters(entity);
        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        int? found = await execution.Connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                Metadata.ExistsSql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));

        return found is not null;
    }

    public async Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        OnBeforeOperation(entity);

        await ExecuteAsync(Metadata.DeleteSql, entity, cancellationToken);
    }

    public async Task SaveAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        OnBeforeOperation(entity);

        await ExecuteAsync(Metadata.SaveSql, entity, cancellationToken);
    }

    protected virtual void OnBeforeOperation(TEntity entity)
    {
    }

    protected async Task<IEnumerable<TResult>> QueryAsync<TResult>(
        string sql,
        DynamicParameters? parameters,
        CancellationToken cancellationToken)
    {
        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        return await execution.Connection.QueryAsync<TResult>(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    private async Task ExecuteAsync(
        string sql,
        TEntity entity,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = CreateParameters(entity);

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        await execution.Connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    private static DynamicParameters CreateParameters(TEntity entity)
    {
        var parameters = new DynamicParameters();

        foreach (LinkColumnMetadata column in Metadata.Columns)
        {
            parameters.Add(
                column.ParameterName,
                column.Property.GetValue(entity));
        }

        return parameters;
    }
}
