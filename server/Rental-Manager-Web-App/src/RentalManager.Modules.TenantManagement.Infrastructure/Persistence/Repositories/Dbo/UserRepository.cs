using System.Data;
using Dapper;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Common;
using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Sessions;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Repositories.Dbo;

/// <summary>
/// Global identity lookup. No organization predicate applies, because a user
/// exists before any organization has been selected.
/// </summary>
public sealed class UserRepository : BaseEntityAuditRepository<User, Guid>, IUserRepository
{
    public UserRepository(
        ISqlExecutionContext executionContext,
        IOrganizationContext organizationContext)
        : base(executionContext, organizationContext)
    {
    }

    public async Task<User?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("NormalizedEmail", normalizedEmail, DbType.String, size: 256);

        string sql = $"""
            SELECT
                {Metadata.SelectColumnList}
            FROM {Metadata.QualifiedTableName}
            WHERE [NormalizedEmail] = @NormalizedEmail
              AND [DeletedAt] IS NULL;
            """;

        return await QuerySingleOrDefaultAsync<User>(
            sql,
            parameters,
            cancellationToken);
    }
}
