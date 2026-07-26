using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

/// <summary>
/// Generic persistence for one entity. Two behaviours are derived from the
/// entity itself rather than configured per repository: an entity that maps
/// <c>OrganizationId</c> is organization scoped and fails closed without a
/// trusted context, and an entity that maps <c>RowVersion</c> requires an
/// expected row version on every update.
/// </summary>
public abstract class BaseRepository<TEntity, TPrimaryKey> :
    IBaseRepository<TEntity, TPrimaryKey>
    where TEntity : class, IEntity<TPrimaryKey>
    where TPrimaryKey : notnull
{
    private protected static readonly EntitySqlMetadata Metadata =
        EntitySqlMetadata.Create<TEntity>();

    private readonly ISqlExecutionContext _executionContext;
    private readonly IOrganizationContext _organizationContext;

    protected BaseRepository(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(organizationContext);

        _executionContext = executionContext;
        _organizationContext = organizationContext;
    }

    /// <summary>
    /// Message catalog object name used when this repository raises a not found
    /// or conflict error.
    /// </summary>
    protected virtual string ObjectName { get; } =
        char.ToLowerInvariant(typeof(TEntity).Name[0]) + typeof(TEntity).Name[1..];

    protected IOrganizationContext OrganizationContext => _organizationContext;

    public async Task<TEntity?> GetAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = CreateKeyParameters(id);

        return await QuerySingleOrDefaultAsync<TEntity>(
            Metadata.SelectByIdSql,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = CreateOrganizationScopedParameters();

        IEnumerable<TEntity> entities = await QueryAsync<TEntity>(
            Metadata.SelectAllSql,
            parameters,
            cancellationToken);

        return entities.ToArray();
    }

    public async Task<byte[]?> InsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ApplyOrganizationOwnership(entity);
        OnBeforeInsert(entity);

        DynamicParameters parameters = CreateEntityParameters(entity);

        byte[]? rowVersion = await WithSqlTranslationAsync(
            () => ExecuteScalarAsync<byte[]?>(
                Metadata.InsertSql,
                parameters,
                cancellationToken));

        if (rowVersion is not null && entity is IConcurrencyAware concurrencyAware)
        {
            concurrencyAware.RowVersion = rowVersion;
        }

        return rowVersion;
    }

    public async Task<byte[]?> UpdateAsync(
        TEntity entity,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (Metadata.UpdateSql is null)
        {
            throw new InvalidOperationException(
                $"Entity '{typeof(TEntity).FullName}' has no updatable column.");
        }

        if (Metadata.IsConcurrencyAware && expectedRowVersion is null)
        {
            throw new InvalidOperationException(
                $"Entity '{typeof(TEntity).FullName}' is concurrency aware, so " +
                "an expected row version is required to update it.");
        }

        ApplyOrganizationOwnership(entity);
        OnBeforeUpdate(entity);

        DynamicParameters parameters = CreateEntityParameters(entity);

        if (Metadata.IsConcurrencyAware)
        {
            AddRowVersionParameter(
                parameters,
                EntitySqlMetadata.ExpectedRowVersionParameterName,
                expectedRowVersion!);
        }

        EntityUpdateResult? result = await WithSqlTranslationAsync(
            () => QuerySingleOrDefaultAsync<EntityUpdateResult>(
                Metadata.UpdateSql,
                parameters,
                cancellationToken));

        if (result is null || result.AffectedRows == 0)
        {
            throw await CreateWriteFailureAsync(entity.Id, cancellationToken);
        }

        if (result.NewRowVersion is not null && entity is IConcurrencyAware concurrencyAware)
        {
            concurrencyAware.RowVersion = result.NewRowVersion;
        }

        return result.NewRowVersion;
    }

    public async Task SaveAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ApplyOrganizationOwnership(entity);

        DynamicParameters parameters = CreateEntityParameters(entity);

        await WithSqlTranslationAsync(
            () => ExecuteAsync(Metadata.SaveSql, parameters, cancellationToken));
    }

    public async Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DynamicParameters parameters = CreateKeyParameters(entity.Id);

        int affectedRows = await ExecuteAsync(
            Metadata.DeleteSql,
            parameters,
            cancellationToken);

        if (affectedRows == 0)
        {
            throw new ResourceNotFoundException(ObjectName);
        }
    }

    /// <summary>
    /// Runs a paged projection over a caller-supplied predicate. The caller owns
    /// the filter and the ordering because both are business decisions, while
    /// the organization predicate, the active-row predicate and the paging
    /// mechanics stay here.
    /// </summary>
    protected async Task<PagedResult<TEntity>> GetPagedAsync(
        string additionalPredicate,
        string orderByClause,
        DynamicParameters parameters,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(orderByClause);

        AddOrganizationParameter(parameters);

        string predicate =
            $"WHERE 1 = 1{Metadata.OrganizationPredicate}{Metadata.ActiveRowPredicate}" +
            additionalPredicate;

        string sql = $"""
            SELECT COUNT_BIG(1)
            FROM {Metadata.QualifiedTableName}
            {predicate};

            SELECT
                {Metadata.SelectColumnList}
            FROM {Metadata.QualifiedTableName}
            {predicate}
            ORDER BY {orderByClause}
            OFFSET @PagingOffset ROWS
            FETCH NEXT @PagingLimit ROWS ONLY;
            """;

        parameters.Add("PagingOffset", request.Offset, DbType.Int64);
        parameters.Add("PagingLimit", request.PageSize, DbType.Int32);

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        await using SqlMapper.GridReader reader = await execution.Connection
            .QueryMultipleAsync(
                new CommandDefinition(
                    sql,
                    parameters,
                    execution.Transaction,
                    cancellationToken: cancellationToken));

        long totalItems = await reader.ReadSingleAsync<long>();
        TEntity[] items = (await reader.ReadAsync<TEntity>()).ToArray();

        return new PagedResult<TEntity>(
            items,
            request.Page,
            request.PageSize,
            (int)Math.Min(totalItems, int.MaxValue));
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

    protected async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        string sql,
        DynamicParameters? parameters,
        CancellationToken cancellationToken)
    {
        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        return await execution.Connection.QuerySingleOrDefaultAsync<TResult>(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    protected async Task<int> ExecuteAsync(
        string sql,
        DynamicParameters? parameters,
        CancellationToken cancellationToken)
    {
        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        return await execution.Connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    protected async Task<TResult?> ExecuteScalarAsync<TResult>(
        string sql,
        DynamicParameters? parameters,
        CancellationToken cancellationToken)
    {
        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        return await execution.Connection.ExecuteScalarAsync<TResult?>(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Gives a derived repository the chance to turn a SQL Server error into a
    /// business error. Returning <see langword="null"/> rethrows the original,
    /// so an unrecognised failure is never reinterpreted.
    /// </summary>
    protected virtual Exception? TranslateSqlException(SqlException exception)
    {
        return null;
    }

    /// <summary>
    /// Runs a write and applies <see cref="TranslateSqlException"/> to any SQL
    /// Server error it raises.
    /// </summary>
    protected async Task<TResult> WithSqlTranslationAsync<TResult>(
        Func<Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            return await action();
        }
        catch (SqlException exception)
        {
            Exception? translated = TranslateSqlException(exception);

            if (translated is null)
            {
                throw;
            }

            throw translated;
        }
    }

    /// <summary>
    /// Hook for cross-cutting write concerns such as audit timestamps.
    /// </summary>
    protected virtual void OnBeforeInsert(TEntity entity)
    {
    }

    /// <summary>
    /// Hook for cross-cutting write concerns such as audit timestamps.
    /// </summary>
    protected virtual void OnBeforeUpdate(TEntity entity)
    {
    }

    /// <summary>
    /// Parameter set carrying only the organization predicate value, for
    /// derived repositories that compose their own SQL.
    /// </summary>
    protected DynamicParameters CreateOrganizationScopedParameters()
    {
        var parameters = new DynamicParameters();
        AddOrganizationParameter(parameters);
        return parameters;
    }

    protected DynamicParameters CreateKeyParameters(TPrimaryKey id)
    {
        var parameters = new DynamicParameters();
        parameters.Add(Metadata.KeyColumn.ParameterName, id);
        AddOrganizationParameter(parameters);
        return parameters;
    }

    protected static void AddRowVersionParameter(
        DynamicParameters parameters,
        string parameterName,
        byte[] rowVersion)
    {
        parameters.Add(parameterName, rowVersion, DbType.Binary, size: 8);
    }

    protected DynamicParameters CreateEntityParameters(TEntity entity)
    {
        var parameters = new DynamicParameters();

        foreach (EntityColumnMetadata column in Metadata.Columns)
        {
            if (!column.IsInsertable && !column.IsUpdatable && !column.IsKey)
            {
                continue;
            }

            parameters.Add(
                column.ParameterName,
                column.Property.GetValue(entity));
        }

        AddOrganizationParameter(parameters);

        return parameters;
    }

    /// <summary>
    /// Resolves the organization for organization-owned entities, and fails
    /// closed when no trusted context is present.
    /// </summary>
    protected Guid RequireOrganizationId()
    {
        return _organizationContext.OrganizationId
            ?? throw new MissingOrganizationContextException();
    }

    private void AddOrganizationParameter(DynamicParameters parameters)
    {
        if (!Metadata.IsOrganizationOwned)
        {
            return;
        }

        if (parameters.ParameterNames.Contains(
                EntitySqlMetadata.OrganizationParameterName,
                StringComparer.Ordinal))
        {
            return;
        }

        parameters.Add(
            EntitySqlMetadata.OrganizationParameterName,
            RequireOrganizationId(),
            DbType.Guid);
    }

    /// <summary>
    /// Stamps the organization from the trusted context so no client-supplied
    /// value can ever reach the database.
    /// </summary>
    private void ApplyOrganizationOwnership(TEntity entity)
    {
        if (!Metadata.IsOrganizationOwned)
        {
            return;
        }

        Guid organizationId = RequireOrganizationId();

        if (entity is IOrganizationOwned organizationOwned)
        {
            organizationOwned.OrganizationId = organizationId;
            return;
        }

        Metadata.OrganizationColumn!.Property.SetValue(entity, organizationId);
    }

    /// <summary>
    /// A write that matched no row is either a missing row or a stale row
    /// version. The distinction is resolved inside the same transaction so the
    /// answer cannot drift.
    /// </summary>
    private async Task<DomainException> CreateWriteFailureAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = CreateKeyParameters(id);

        int? exists = await ExecuteScalarAsync<int?>(
            Metadata.ExistsSql,
            parameters,
            cancellationToken);

        return exists is null
            ? new ResourceNotFoundException(ObjectName)
            : new ConcurrencyConflictException(ObjectName);
    }
}
