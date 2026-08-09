using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Contracts;

namespace RentalManager.Modules.Identity.Infrastructure.Persistence;

public sealed class OrganizationMembershipReader(
    IIdentityConnectionFactory connectionFactory)
    : IOrganizationMembershipReader
{
    public async Task<IReadOnlyList<OrganizationOptionDto>> ListActiveOrganizationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        IEnumerable<MembershipRow> rows = await connection.QueryAsync<MembershipRow>(
            new CommandDefinition(
                IdentitySqlStatements.ListActiveOrganizations,
                new { UserId = userId },
                cancellationToken: cancellationToken));

        return rows
            .Select(row => new OrganizationOptionDto(row.OrganizationId, row.Name))
            .ToArray();
    }

    public async Task<bool> IsActiveMemberAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrganizationOptionDto> organizations =
            await ListActiveOrganizationsAsync(userId, cancellationToken);

        return organizations.Any(organization => organization.Id == organizationId);
    }

    private sealed class MembershipRow
    {
        public Guid OrganizationId { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
