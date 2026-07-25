using Dapper;
using Microsoft.Data.SqlClient;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Connection pooling is where tenant isolation is easiest to get wrong: a
/// recycled connection can still carry the previous caller's session context.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class ConnectionPoolIsolationTests
{
    private readonly SqlServerFixture _fixture;

    public ConnectionPoolIsolationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Alternating_organizations_on_one_pool_never_see_each_other()
    {
        await _fixture.ResetOrgDataAsync();

        await using TenantScope tenantA = TenantScope.For(
            _fixture,
            TestData.OrganizationA.Id,
            TestData.Users.AdministratorAId);

        await using TenantScope tenantB = TenantScope.For(
            _fixture,
            TestData.OrganizationB.Id,
            TestData.Users.AdministratorBId);

        await tenantA.CreateTextFieldAsync("room_number_a");
        await tenantB.CreateTextFieldAsync("room_number_b");

        // Pools are warm at this point, so the following units of work are very
        // likely to be handed a connection last used by the other tenant.
        for (int iteration = 0; iteration < 10; iteration++)
        {
            PagedResult<FieldDto> fromA = await tenantA.ListAsync(new GetFieldsRequest());
            PagedResult<FieldDto> fromB = await tenantB.ListAsync(new GetFieldsRequest());

            Assert.Equal("room_number_a", Assert.Single(fromA.Items).Key);
            Assert.Equal("room_number_b", Assert.Single(fromB.Items).Key);
        }
    }

    [Fact]
    public async Task A_recycled_connection_does_not_carry_the_previous_context()
    {
        await _fixture.ResetOrgDataAsync();

        SqlConnection.ClearAllPools();

        await using (SqlConnection scoped =
            await _fixture.OpenConnectionAsync(TestData.OrganizationA.Id))
        {
            Assert.Equal(
                TestData.OrganizationA.Id,
                await ReadSessionOrganizationAsync(scoped));
        }

        // Same pooled connection, taken without publishing an organization.
        await using SqlConnection recycled = await _fixture.OpenConnectionAsync();

        Assert.Null(await ReadSessionOrganizationAsync(recycled));
    }

    private static Task<Guid?> ReadSessionOrganizationAsync(SqlConnection connection)
    {
        return connection.ExecuteScalarAsync<Guid?>(
            "SELECT CAST(SESSION_CONTEXT(N'OrganizationId') AS UNIQUEIDENTIFIER);");
    }
}
