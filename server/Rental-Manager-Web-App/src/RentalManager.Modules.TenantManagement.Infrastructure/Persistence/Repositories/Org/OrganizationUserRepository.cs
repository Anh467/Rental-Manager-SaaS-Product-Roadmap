using System.Data;
using Dapper;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Org;

/// <summary>
/// Organization membership persistence. Scoped by the trusted organization
/// context and row level security; never trusts a client-supplied organization.
/// </summary>
public sealed class OrganizationUserRepository : IOrganizationUserRepository
{
    private readonly ISqlExecutionContext _executionContext;
    private readonly IOrganizationContext _organizationContext;

    public OrganizationUserRepository(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(organizationContext);

        _executionContext = executionContext;
        _organizationContext = organizationContext;
    }

    public async Task<OrganizationUser?> FindActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Guid organizationId = RequireOrganizationId();
        var parameters = CreateParameters(userId, organizationId);

        const string sql = """
            SELECT
                membership.[OrganizationId],
                membership.[UserId],
                membership.[RoleId],
                membership.[IsActive],
                membership.[CreatedAt],
                membership.[UpdatedAt]
            FROM [org].[OrganizationUser] AS membership
            INNER JOIN [dbo].[User] AS [user]
                ON [user].[Id] = membership.[UserId]
            INNER JOIN [org].[Role] AS role
                ON role.[Id] = membership.[RoleId]
               AND role.[OrganizationId] = membership.[OrganizationId]
            WHERE membership.[OrganizationId] = @OrganizationId
              AND membership.[UserId] = @UserId
              AND membership.[IsActive] = 1
              AND [user].[IsActive] = 1
              AND [user].[DeletedAt] IS NULL
              AND role.[IsActive] = 1
              AND role.[DeletedAt] IS NULL;
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        return await execution.Connection.QuerySingleOrDefaultAsync<OrganizationUser>(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task<Guid?> GetActiveRoleIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        OrganizationUser? membership = await FindActiveByUserIdAsync(
            userId,
            cancellationToken);

        return membership?.RoleId;
    }

    public async Task<IReadOnlyList<ActiveOrganizationMembership>> ListActiveMembershipsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Guid);

        const string sql = """
            EXEC [dbo].[usp_ListActiveOrganizationMembershipsForUser] @UserId = @UserId;
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        IEnumerable<ActiveOrganizationMembership> memberships =
            await execution.Connection.QueryAsync<ActiveOrganizationMembership>(
                new CommandDefinition(
                    sql,
                    parameters,
                    execution.Transaction,
                    cancellationToken: cancellationToken));

        return memberships.ToArray();
    }

    public async Task SaveAsync(
        OrganizationUser membership,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membership);

        Guid organizationId = RequireOrganizationId();
        membership.OrganizationId = organizationId;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (membership.CreatedAt == default)
        {
            membership.CreatedAt = now;
        }

        membership.UpdatedAt = now;

        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", membership.OrganizationId, DbType.Guid);
        parameters.Add("UserId", membership.UserId, DbType.Guid);
        parameters.Add("RoleId", membership.RoleId, DbType.Guid);
        parameters.Add("IsActive", membership.IsActive, DbType.Boolean);
        parameters.Add("CreatedAt", membership.CreatedAt);
        parameters.Add("UpdatedAt", membership.UpdatedAt);

        const string sql = """
            MERGE [org].[OrganizationUser] AS target
            USING
            (
                SELECT
                    @OrganizationId AS [OrganizationId],
                    @UserId AS [UserId],
                    @RoleId AS [RoleId],
                    @IsActive AS [IsActive],
                    @CreatedAt AS [CreatedAt],
                    @UpdatedAt AS [UpdatedAt]
            ) AS source
            ON target.[OrganizationId] = source.[OrganizationId]
               AND target.[UserId] = source.[UserId]
            WHEN MATCHED THEN
                UPDATE SET
                    [RoleId] = source.[RoleId],
                    [IsActive] = source.[IsActive],
                    [UpdatedAt] = source.[UpdatedAt]
            WHEN NOT MATCHED THEN
                INSERT
                (
                    [OrganizationId],
                    [UserId],
                    [RoleId],
                    [IsActive],
                    [CreatedAt],
                    [UpdatedAt]
                )
                VALUES
                (
                    source.[OrganizationId],
                    source.[UserId],
                    source.[RoleId],
                    source.[IsActive],
                    source.[CreatedAt],
                    source.[UpdatedAt]
                );
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        await execution.Connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Guid organizationId = RequireOrganizationId();
        DynamicParameters parameters = CreateParameters(userId, organizationId);

        const string sql = """
            DELETE FROM [org].[OrganizationUser]
            WHERE [OrganizationId] = @OrganizationId
              AND [UserId] = @UserId;
            """;

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        await execution.Connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }

    private Guid RequireOrganizationId()
    {
        return _organizationContext.OrganizationId
            ?? throw new MissingOrganizationContextException();
    }

    private static DynamicParameters CreateParameters(
        Guid userId,
        Guid organizationId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Guid);
        parameters.Add("OrganizationId", organizationId, DbType.Guid);
        return parameters;
    }
}
