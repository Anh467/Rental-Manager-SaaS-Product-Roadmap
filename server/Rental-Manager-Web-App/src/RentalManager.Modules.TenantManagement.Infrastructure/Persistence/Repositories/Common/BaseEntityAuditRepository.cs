using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Connections;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

public abstract class BaseEntityAuditRepository<TEntity, TPrimaryKey> :
    BaseRepository<TEntity, TPrimaryKey>,
    IBaseEntityAuditRepository<TEntity, TPrimaryKey>
    where TEntity : class, IEntityAudit<TPrimaryKey>
    where TPrimaryKey : notnull
{
    private static readonly EntityColumnMetadata DeletedAtColumn =
        Metadata.FindColumn(nameof(IAudit.DeletedAt))
        ?? throw new InvalidOperationException(
            $"Auditable entity '{typeof(TEntity).FullName}' must map DeletedAt.");

    private static readonly EntityColumnMetadata? UpdatedAtColumn =
        Metadata.FindColumn(nameof(IAudit.UpdatedAt));

    private static readonly string SoftDeleteSql = BuildSoftDeleteSql();

    protected BaseEntityAuditRepository(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task SoftDeleteAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset deletedAt = DateTimeOffset.UtcNow;
        var parameters = new DynamicParameters();
        parameters.Add(Metadata.KeyProperty.Name, id);
        parameters.Add(DeletedAtColumn.ParameterName, deletedAt);

        if (UpdatedAtColumn is not null)
        {
            parameters.Add(UpdatedAtColumn.ParameterName, deletedAt);
        }

        await using SqlConnection connection =
            await ConnectionFactory.OpenConnectionAsync(cancellationToken);

        int affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                SoftDeleteSql,
                parameters,
                cancellationToken: cancellationToken));

        if (affectedRows == 0)
        {
            throw new KeyNotFoundException(
                $"{typeof(TEntity).Name} with id '{id}' was not found or was already deleted.");
        }
    }

    private static string BuildSoftDeleteSql()
    {
        EntityColumnMetadata keyColumn = Metadata.Columns.Single(column => column.IsKey);

        string assignments = UpdatedAtColumn is null
            ? $"{DeletedAtColumn.QuotedColumnName} = @{DeletedAtColumn.ParameterName}"
            : $"{DeletedAtColumn.QuotedColumnName} = @{DeletedAtColumn.ParameterName},\n" +
              $"    {UpdatedAtColumn.QuotedColumnName} = @{UpdatedAtColumn.ParameterName}";

        return $"""
            UPDATE {Metadata.QualifiedTableName}
            SET
                {assignments}
            WHERE {keyColumn.QuotedColumnName} = @{keyColumn.ParameterName}
              AND {DeletedAtColumn.QuotedColumnName} IS NULL;
            """;
    }
}
