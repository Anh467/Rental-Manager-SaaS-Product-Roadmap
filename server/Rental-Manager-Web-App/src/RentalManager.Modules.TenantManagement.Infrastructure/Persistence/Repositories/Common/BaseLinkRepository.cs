using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Connections;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

internal class BaseLinkRepository<TEntity> : ILinkRepository<TEntity>
    where TEntity : class, ILink
{
    private static readonly LinkSqlMetadata Metadata =
        LinkSqlMetadata.Create<TEntity>();

    private readonly ISqlConnectionFactory _connectionFactory;

    protected BaseLinkRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DynamicParameters parameters = CreateParameters(entity);

        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                Metadata.DeleteSql,
                parameters,
                cancellationToken: cancellationToken));
    }

    public async Task SaveAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DynamicParameters parameters = CreateParameters(entity);

        await using SqlConnection connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                Metadata.SaveSql,
                parameters,
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
