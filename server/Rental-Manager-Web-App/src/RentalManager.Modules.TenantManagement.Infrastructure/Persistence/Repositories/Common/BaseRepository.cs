using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Connections;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

public abstract class BaseRepository<TEntity, TPrimaryKey> :
    IBaseRepository<TEntity, TPrimaryKey>
    where TEntity : class, IEntity<TPrimaryKey>
    where TPrimaryKey : notnull
{
    private protected static readonly EntitySqlMetadata Metadata =
        EntitySqlMetadata.Create<TEntity>();

    private readonly ISqlConnectionFactory _connectionFactory;

    protected BaseRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var parameters = new DynamicParameters();
        parameters.Add(Metadata.KeyProperty.Name, entity.Id);

        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        int affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                Metadata.DeleteSql,
                parameters,
                cancellationToken: cancellationToken));

        if (affectedRows == 0)
        {
            throw CreateNotFoundException(entity.Id);
        }
    }

    public async Task<IReadOnlyCollection<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        IEnumerable<TEntity> entities = await connection.QueryAsync<TEntity>(
            new CommandDefinition(
                Metadata.SelectAllSql,
                cancellationToken: cancellationToken));

        return entities.ToArray();
    }

    public async Task<TEntity> GetAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add(Metadata.KeyProperty.Name, id);

        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        TEntity? entity = await connection.QuerySingleOrDefaultAsync<TEntity>(
            new CommandDefinition(
                Metadata.SelectByIdSql,
                parameters,
                cancellationToken: cancellationToken));

        return entity ?? throw CreateNotFoundException(id);
    }

    public async Task SaveAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DynamicParameters parameters = CreateEntityParameters(entity);

        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                Metadata.SaveSql,
                parameters,
                cancellationToken: cancellationToken));
    }

    protected ISqlConnectionFactory ConnectionFactory => _connectionFactory;

    protected static DynamicParameters CreateEntityParameters(TEntity entity)
    {
        var parameters = new DynamicParameters();

        foreach (EntityColumnMetadata column in Metadata.Columns)
        {
            parameters.Add(
                column.ParameterName,
                column.Property.GetValue(entity));
        }

        return parameters;
    }

    private static KeyNotFoundException CreateNotFoundException(object id)
    {
        return new KeyNotFoundException(
            $"{typeof(TEntity).Name} with id '{id}' was not found.");
    }
}
