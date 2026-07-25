using System.Data;
using Dapper;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Authorization;

/// <summary>
/// Resolves membership and permissions for the organization currently bound to
/// the session. Row level security on <c>[org].[OrganizationUser]</c> and
/// <c>[org].[RolePermission]</c> means these queries can only ever see the
/// caller's own organization.
/// </summary>
public sealed class PermissionReader : IPermissionReader
{
    private const string MembershipSql = """
        SELECT TOP (1) 1
        FROM [org].[OrganizationUser] AS organizationUser
        INNER JOIN [org].[Role] AS role
            ON role.[Id] = organizationUser.[RoleId]
           AND role.[OrganizationId] = organizationUser.[OrganizationId]
        WHERE organizationUser.[UserId] = @UserId
          AND organizationUser.[IsActive] = 1
          AND role.[IsActive] = 1
          AND role.[DeletedAt] IS NULL;
        """;

    private const string PermissionsSql = """
        SELECT permission.[Code]
        FROM [org].[OrganizationUser] AS organizationUser
        INNER JOIN [org].[Role] AS role
            ON role.[Id] = organizationUser.[RoleId]
           AND role.[OrganizationId] = organizationUser.[OrganizationId]
        INNER JOIN [org].[RolePermission] AS rolePermission
            ON rolePermission.[RoleId] = role.[Id]
           AND rolePermission.[OrganizationId] = role.[OrganizationId]
        INNER JOIN [dbo].[Permission] AS permission
            ON permission.[Id] = rolePermission.[PermissionId]
        WHERE organizationUser.[UserId] = @UserId
          AND organizationUser.[IsActive] = 1
          AND role.[IsActive] = 1
          AND role.[DeletedAt] IS NULL
          AND permission.[IsActive] = 1;
        """;

    private readonly ISqlExecutionContext _executionContext;

    public PermissionReader(ISqlExecutionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        _executionContext = executionContext;
    }

    public async Task<bool> IsActiveMemberAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        int? found = await execution.Connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                MembershipSql,
                CreateParameters(userId),
                execution.Transaction,
                cancellationToken: cancellationToken));

        return found is not null;
    }

    public async Task<IReadOnlySet<string>> GetPermissionCodesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        IEnumerable<string> codes = await execution.Connection.QueryAsync<string>(
            new CommandDefinition(
                PermissionsSql,
                CreateParameters(userId),
                execution.Transaction,
                cancellationToken: cancellationToken));

        return codes.ToHashSet(StringComparer.Ordinal);
    }

    private static DynamicParameters CreateParameters(Guid userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Guid);
        return parameters;
    }
}
