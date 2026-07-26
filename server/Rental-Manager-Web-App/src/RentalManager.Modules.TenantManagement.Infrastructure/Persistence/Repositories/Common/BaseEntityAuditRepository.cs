using Dapper;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Domain.Entities.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;

/// <summary>
/// Adds the audit concerns shared by every auditable entity: write-time
/// timestamps and soft delete that honours organization scope and optimistic
/// concurrency.
/// </summary>
public abstract class BaseEntityAuditRepository<TEntity, TPrimaryKey> :
    BaseRepository<TEntity, TPrimaryKey>,
    IBaseEntityAuditRepository<TEntity, TPrimaryKey>
    where TEntity : class, IEntityAudit<TPrimaryKey>
    where TPrimaryKey : notnull
{
    private static readonly EntityColumnMetadata DeletedAtColumn =
        Metadata.DeletedAtColumn
        ?? throw new InvalidOperationException(
            $"Auditable entity '{typeof(TEntity).FullName}' must map DeletedAt.");

    private static readonly EntityColumnMetadata? UpdatedAtColumn =
        Metadata.FindColumn(SqlColumnConventions.UpdatedAtPropertyName);

    private static readonly string SoftDeleteSql = BuildSoftDeleteSql();

    protected BaseEntityAuditRepository(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
        : base(executionContext, organizationContext)
    {
    }

    /// <summary>
    /// Overridable so tests can pin time; production uses the wall clock.
    /// </summary>
    protected virtual DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public async Task SoftDeleteAsync(
        TPrimaryKey id,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (Metadata.IsConcurrencyAware && expectedRowVersion is null)
        {
            throw new InvalidOperationException(
                $"Entity '{typeof(TEntity).FullName}' is concurrency aware, so " +
                "an expected row version is required to soft delete it.");
        }

        DateTimeOffset deletedAt = UtcNow;
        DynamicParameters parameters = CreateKeyParameters(id);
        parameters.Add(DeletedAtColumn.ParameterName, deletedAt);

        if (UpdatedAtColumn is not null)
        {
            parameters.Add(UpdatedAtColumn.ParameterName, deletedAt);
        }

        if (Metadata.IsConcurrencyAware)
        {
            AddRowVersionParameter(
                parameters,
                EntitySqlMetadata.ExpectedRowVersionParameterName,
                expectedRowVersion!);
        }

        int affectedRows = await ExecuteAsync(
            SoftDeleteSql,
            parameters,
            cancellationToken);

        if (affectedRows == 0)
        {
            throw await CreateSoftDeleteFailureAsync(id, cancellationToken);
        }
    }

    protected override void OnBeforeInsert(TEntity entity)
    {
        DateTimeOffset now = UtcNow;

        if (entity.CreatedAt == default)
        {
            entity.CreatedAt = now;
        }

        entity.UpdatedAt = now;
    }

    protected override void OnBeforeUpdate(TEntity entity)
    {
        entity.UpdatedAt = UtcNow;
    }

    private async Task<Exception> CreateSoftDeleteFailureAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = CreateKeyParameters(id);

        int? exists = await ExecuteScalarAsync<int?>(
            Metadata.ExistsSql,
            parameters,
            cancellationToken);

        return exists is null
            ? new Core.Exceptions.ResourceNotFoundException(ObjectName)
            : new Core.Exceptions.ConcurrencyConflictException(ObjectName);
    }

    private static string BuildSoftDeleteSql()
    {
        string assignments = UpdatedAtColumn is null
            ? $"{DeletedAtColumn.QuotedColumnName} = @{DeletedAtColumn.ParameterName}"
            : $"{DeletedAtColumn.QuotedColumnName} = @{DeletedAtColumn.ParameterName},\n" +
              $"    {UpdatedAtColumn.QuotedColumnName} = @{UpdatedAtColumn.ParameterName}";

        string concurrencyPredicate = Metadata.RowVersionColumn is null
            ? string.Empty
            : $"\n  AND {Metadata.RowVersionColumn.QuotedColumnName} = " +
              $"@{EntitySqlMetadata.ExpectedRowVersionParameterName}";

        return $"""
            UPDATE {Metadata.QualifiedTableName}
            SET
                {assignments}
            WHERE {Metadata.KeyColumn.QuotedColumnName} = @{Metadata.KeyColumn.ParameterName}{Metadata.OrganizationPredicate}{concurrencyPredicate}
              AND {DeletedAtColumn.QuotedColumnName} IS NULL;
            """;
    }
}
