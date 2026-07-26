using System.Net;
using System.Net.Http.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Login-time membership discovery must see <c>[org].[OrganizationUser]</c>
/// through <c>OrganizationMembershipResolver</c> without weakening FILTER or
/// BLOCK predicates on other tenant tables.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class MembershipResolverRlsTests
{
    private readonly SqlServerFixture _fixture;

    public MembershipResolverRlsTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Direct_organization_user_query_without_context_returns_no_rows()
    {
        await using SqlConnection connection = await _fixture.OpenConnectionAsync();

        Assert.Equal(
            0,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT_BIG(1) FROM [org].[OrganizationUser];"));
    }

    [Fact]
    public async Task Membership_procedure_returns_only_the_requested_users_memberships()
    {
        await using TenantScope unbound = TenantScope.Unbound(_fixture);
        await using AsyncServiceScope scope = unbound.BeginUnitOfWork();

        IOrganizationUserRepository memberships =
            scope.ServiceProvider.GetRequiredService<IOrganizationUserRepository>();

        IReadOnlyList<ActiveOrganizationMembership> forAdministratorA =
            await memberships.ListActiveMembershipsByUserIdAsync(
                TestData.Users.AdministratorAId);

        ActiveOrganizationMembership membership = Assert.Single(forAdministratorA);
        Assert.Equal(TestData.OrganizationA.Id, membership.OrganizationId);
        Assert.Equal(TestData.OrganizationA.AdministratorRoleId, membership.RoleId);

        IReadOnlyList<ActiveOrganizationMembership> forAdministratorB =
            await memberships.ListActiveMembershipsByUserIdAsync(
                TestData.Users.AdministratorBId);

        Assert.Equal(
            TestData.OrganizationB.Id,
            Assert.Single(forAdministratorB).OrganizationId);

        Assert.DoesNotContain(
            forAdministratorA,
            item => item.OrganizationId == TestData.OrganizationB.Id);
        Assert.DoesNotContain(
            forAdministratorB,
            item => item.OrganizationId == TestData.OrganizationA.Id);
    }

    [Fact]
    public async Task Single_membership_user_can_login_without_organization_id()
    {
        using var factory = new FieldsApiFactory(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = TestData.Users.AdministratorAEmail,
                password = TestData.Password
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        TokenEnvelope envelope =
            await response.Content.ReadFromJsonAsync<TokenEnvelope>(FieldsApiFactory.Json)
            ?? throw new InvalidOperationException("Empty login body.");

        Assert.False(string.IsNullOrWhiteSpace(envelope.Data.AccessToken));
        Assert.Equal("Bearer", envelope.Data.TokenType);
    }

    [Fact]
    public async Task Membership_resolver_does_not_bypass_field_or_role_filters()
    {
        await _fixture.ResetOrgDataAsync();

        await using TenantScope tenantA = TenantScope.For(
            _fixture,
            TestData.OrganizationA.Id,
            TestData.Users.AdministratorAId);

        await tenantA.CreateTextFieldAsync("room_number");

        await using SqlConnection connection = await _fixture.OpenConnectionAsync();

        await connection.ExecuteAsync(
            "EXECUTE AS USER = N'OrganizationMembershipResolver';");

        try
        {
            // No SELECT grant on tenant tables other than OrganizationUser.
            SqlException fieldDenied = await Assert.ThrowsAsync<SqlException>(
                () => connection.ExecuteScalarAsync<long>(
                    "SELECT COUNT_BIG(1) FROM [org].[Field];"));
            Assert.Equal(229, fieldDenied.Number);

            SqlException roleDenied = await Assert.ThrowsAsync<SqlException>(
                () => connection.ExecuteScalarAsync<long>(
                    "SELECT COUNT_BIG(1) FROM [org].[Role];"));
            Assert.Equal(229, roleDenied.Number);

            Assert.True(
                await connection.ExecuteScalarAsync<long>(
                    "SELECT COUNT_BIG(1) FROM [org].[OrganizationUser];") > 0);
        }
        finally
        {
            await connection.ExecuteAsync("REVERT;");
        }
    }

    [Fact]
    public async Task Block_predicate_still_rejects_cross_tenant_membership_insert()
    {
        await using SqlConnection connection =
            await _fixture.OpenConnectionAsync(TestData.OrganizationA.Id);

        SqlException exception = await Assert.ThrowsAsync<SqlException>(
            () => connection.ExecuteAsync(
                """
                INSERT INTO [org].[OrganizationUser]
                    ([OrganizationId], [UserId], [RoleId], [CreatedAt], [UpdatedAt])
                VALUES
                    (@OrganizationId, @UserId, @RoleId, @Now, @Now);
                """,
                new
                {
                    OrganizationId = TestData.OrganizationB.Id,
                    UserId = TestData.Users.AdministratorAId,
                    RoleId = TestData.OrganizationB.AdministratorRoleId,
                    Now = DateTimeOffset.UtcNow
                }));

        Assert.Equal(33504, exception.Number);
    }

    private sealed record TokenEnvelope(bool Success, string MessageKey, TokenPayload Data);

    private sealed record TokenPayload(string AccessToken, string TokenType);
}
