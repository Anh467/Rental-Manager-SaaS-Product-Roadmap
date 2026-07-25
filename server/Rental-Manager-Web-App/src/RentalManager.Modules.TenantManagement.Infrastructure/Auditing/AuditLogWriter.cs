using System.Data;
using Dapper;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Auditing;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Auditing;

/// <summary>
/// Appends to <c>[org].[AuditLog]</c> on the ambient session, so an entry lives
/// or dies with the transaction that produced it.
/// </summary>
public sealed class AuditLogWriter : IAuditLogWriter
{
    private const int MaxChangeSummaryLength = 2000;

    private const string InsertSql = """
        INSERT INTO [org].[AuditLog]
        (
            [Id],
            [OrganizationId],
            [EntityType],
            [EntityId],
            [Action],
            [PerformedByUserId],
            [PerformedAt],
            [ChangeSummary],
            [CorrelationId]
        )
        VALUES
        (
            @Id,
            @OrganizationId,
            @EntityType,
            @EntityId,
            @Action,
            @PerformedByUserId,
            @PerformedAt,
            @ChangeSummary,
            @CorrelationId
        );
        """;

    private readonly ISqlExecutionContext _executionContext;
    private readonly IOrganizationContext _organizationContext;

    public AuditLogWriter(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(organizationContext);

        _executionContext = executionContext;
        _organizationContext = organizationContext;
    }

    public async Task WriteAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Guid organizationId = _organizationContext.OrganizationId
            ?? throw new MissingOrganizationContextException();

        var parameters = new DynamicParameters();
        parameters.Add("Id", Guid.CreateVersion7(), DbType.Guid);
        parameters.Add("OrganizationId", organizationId, DbType.Guid);
        parameters.Add("EntityType", entry.EntityType, DbType.String, size: 64);
        parameters.Add("EntityId", entry.EntityId, DbType.Guid);
        parameters.Add("Action", entry.Action, DbType.String, size: 32);
        parameters.Add(
            "PerformedByUserId",
            _organizationContext.UserId,
            DbType.Guid);
        parameters.Add("PerformedAt", DateTimeOffset.UtcNow);
        parameters.Add(
            "ChangeSummary",
            Truncate(entry.ChangeSummary, MaxChangeSummaryLength),
            DbType.String,
            size: MaxChangeSummaryLength);
        parameters.Add(
            "CorrelationId",
            _organizationContext.CorrelationId,
            DbType.String,
            size: 128);

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        await execution.Connection.ExecuteAsync(
            new CommandDefinition(
                InsertSql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
