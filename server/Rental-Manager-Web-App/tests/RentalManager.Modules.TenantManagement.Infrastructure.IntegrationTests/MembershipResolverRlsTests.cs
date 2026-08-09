using System.Net;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Login-time membership discovery must see <c>[org].[StaffMembership]</c>
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
    public async Task Direct_staff_membership_query_without_context_returns_no_rows()
    {
        await using SqlConnection connection = await _fixture.OpenConnectionAsync();

        Assert.Equal(
            0,
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT_BIG(1) FROM [org].[StaffMembership];"));
    }

    [Fact]
    public async Task Membership_procedure_returns_only_the_requested_users_memberships()
    {
        await using TenantScope unbound = TenantScope.Unbound(_fixture);
        await using AsyncServiceScope scope = unbound.BeginUnitOfWork();

        IStaffMembershipRepository memberships =
            scope.ServiceProvider.GetRequiredService<IStaffMembershipRepository>();

        IReadOnlyList<ActiveOrganizationMembership> forAdministratorA =
            await memberships.ListActiveMembershipsByUserIdAsync(
                TestData.Users.AdministratorAId);

        ActiveOrganizationMembership membership = Assert.Single(forAdministratorA);
        Assert.Equal(TestData.OrganizationA.Id, membership.OrganizationId);
        Assert.NotEqual(Guid.Empty, membership.StaffMembershipId);

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
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        HttpResponseMessage csrfResponse = await client.GetAsync("/api/v1/auth/csrf");
        csrfResponse.EnsureSuccessStatusCode();
        using var csrfDocument = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        string csrfToken = csrfDocument.RootElement
            .GetProperty("data")
            .GetProperty("requestToken")
            .GetString()
            ?? throw new InvalidOperationException("Missing CSRF token.");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);

        FieldsApiFactory.AttachExternalIdentityHeaders(
            client,
            TestData.Users.AdministratorASubject,
            TestData.Users.AdministratorAEmail);

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/auth/login",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(
            "organization",
            document.RootElement.GetProperty("data").GetProperty("scope").GetString());
        Assert.False(
            document.RootElement.GetProperty("data").TryGetProperty("accessToken", out _));
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
            // No SELECT grant on tenant tables other than StaffMembership.
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
                    "SELECT COUNT_BIG(1) FROM [org].[StaffMembership];") > 0);
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
                INSERT INTO [org].[StaffMembership]
                    ([Id], [OrganizationId], [UserId], [Status], [CreatedAt], [UpdatedAt])
                VALUES
                    (@Id, @OrganizationId, @UserId, 2, @Now, @Now);
                """,
                new
                {
                    Id = Guid.CreateVersion7(),
                    OrganizationId = TestData.OrganizationB.Id,
                    UserId = TestData.Users.AdministratorAId,
                    Now = DateTimeOffset.UtcNow
                }));

        Assert.Equal(33504, exception.Number);
    }
}
