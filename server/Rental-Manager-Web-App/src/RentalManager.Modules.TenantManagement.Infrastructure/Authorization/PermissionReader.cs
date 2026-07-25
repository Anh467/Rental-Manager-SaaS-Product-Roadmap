using System.Data;
using Dapper;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Authorization;

/// <summary>
/// Resolves the role assigned directly to the global user for the organization
/// currently bound to the session, then reads that role's global permission keys.
/// </summary>
public sealed class PermissionReader : IPermissionReader
{
    private const string ActiveRoleSql = """
        SELECT TOP (1) [user].[RoleId]
        FROM [dbo].[User] AS [user]
        INNER JOIN [org].[Role] AS role
            ON role.[Id] = [user].[RoleId]
           AND role.[OrganizationId] = [user].[OrganizationId]
        WHERE [user].[Id] = @UserId
          AND [user].[OrganizationId] = @OrganizationId
          AND [user].[IsActive] = 1
          AND [user].[DeletedAt] IS NULL
          AND role.[IsActive] = 1
          AND role.[DeletedAt] IS NULL;
        """;

    private readonly ISqlExecutionContext _executionContext;
    private readonly IOrganizationContext _organizationContext;
    private readonly IRolePermissionRepository _rolePermissions;

    public PermissionReader(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext,
        IRolePermissionRepository rolePermissions)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(organizationContext);
        ArgumentNullException.ThrowIfNull(rolePermissions);

        _executionContext = executionContext;
        _organizationContext = organizationContext;
        _rolePermissions = rolePermissions;
    }

    public async Task<bool> IsActiveMemberAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await ResolveActiveRoleIdAsync(userId, cancellationToken) is not null;
    }

    public async Task<IReadOnlySet<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Guid? roleId = await ResolveActiveRoleIdAsync(userId, cancellationToken);

        if (roleId is null)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        IReadOnlyCollection<string> keys =
            await _rolePermissions.GetPermissionKeysByRoleAsync(
                roleId.Value,
                cancellationToken);

        return keys.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<Guid?> ResolveActiveRoleIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        Guid organizationId = _organizationContext.OrganizationId
            ?? throw new MissingOrganizationContextException();

        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Guid);
        parameters.Add("OrganizationId", organizationId, DbType.Guid);

        SqlExecution execution = await _executionContext.GetAsync(cancellationToken);

        return await execution.Connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                ActiveRoleSql,
                parameters,
                execution.Transaction,
                cancellationToken: cancellationToken));
    }
}
