using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Concurrent first-login for the same external identity must converge on a
/// single User + UserIdentity pair instead of EmailConflict races.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class FirstLoginConcurrencyTests
{
    private readonly SqlServerFixture _fixture;

    public FirstLoginConcurrencyTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Parallel_first_login_same_provider_subject_email_creates_one_user()
    {
        await _fixture.ResetProvisionedUsersAsync();

        string subject = $"concurrent-subject-{Guid.CreateVersion7():N}";
        string email = $"concurrent.{Guid.CreateVersion7():N}@rentalmanager.test";
        var request = new ExternalUserProvisionRequest(
            TestData.Provider,
            subject,
            email,
            "Concurrent User");

        var stores = Enumerable.Range(0, 8)
            .Select(_ => new SqlUserAccountStore(
                new IdentityConnectionFactory(_fixture.ConnectionString)))
            .ToArray();

        ExternalUserProvisionResult[] results = await Task.WhenAll(
            stores.Select(store => store.ProvisionAsync(request)));

        Assert.All(
            results,
            result => Assert.True(
                result.Status is ExternalUserProvisionStatus.Created
                    or ExternalUserProvisionStatus.AlreadyMapped,
                $"Unexpected status {result.Status}"));

        Assert.Contains(results, result => result.Status == ExternalUserProvisionStatus.Created);
        Assert.DoesNotContain(
            results,
            result => result.Status == ExternalUserProvisionStatus.EmailConflict);

        Guid[] userIds = results
            .Select(result => result.Mapping!.User.UserId)
            .Distinct()
            .ToArray();

        Assert.Single(userIds);

        await using SqlConnection connection = await _fixture.OpenConnectionAsync();

        int userCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT_BIG(1)
            FROM [dbo].[User]
            WHERE [NormalizedEmail] = @NormalizedEmail
              AND [DeletedAt] IS NULL;
            """,
            new { NormalizedEmail = email.ToUpperInvariant() });

        int identityCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT_BIG(1)
            FROM [dbo].[UserIdentity]
            WHERE [Provider] = @Provider
              AND [Subject] = @Subject;
            """,
            new { Provider = TestData.Provider, Subject = subject });

        Assert.Equal(1, userCount);
        Assert.Equal(1, identityCount);
    }
}

/// <summary>
/// Login-time membership listing requires an active StaffRole joined to an
/// active org.Role; session revalidation follows the same USP.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class ActiveRoleMembershipTests
{
    private readonly SqlServerFixture _fixture;

    public ActiveRoleMembershipTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Active_role_membership_is_listed()
    {
        await using TenantScope unbound = TenantScope.Unbound(_fixture);
        await using AsyncServiceScope scope = unbound.BeginUnitOfWork();

        IStaffMembershipRepository memberships =
            scope.ServiceProvider.GetRequiredService<IStaffMembershipRepository>();

        IReadOnlyList<ActiveOrganizationMembership> listed =
            await memberships.ListActiveMembershipsByUserIdAsync(
                TestData.Users.AdministratorAId);

        Assert.Contains(
            listed,
            item => item.OrganizationId == TestData.OrganizationA.Id);
    }

    [Fact]
    public async Task Inactive_org_role_excludes_membership_from_list()
    {
        await using (SqlConnection connection =
                         await _fixture.OpenConnectionAsync(TestData.OrganizationA.Id))
        {
            await connection.ExecuteAsync(
                """
                UPDATE [org].[Role]
                SET [IsActive] = 0, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @RoleId;
                """,
                new { RoleId = TestData.OrganizationA.ViewerRoleId });
        }

        try
        {
            await using TenantScope unbound = TenantScope.Unbound(_fixture);
            await using AsyncServiceScope scope = unbound.BeginUnitOfWork();

            IStaffMembershipRepository memberships =
                scope.ServiceProvider.GetRequiredService<IStaffMembershipRepository>();

            IReadOnlyList<ActiveOrganizationMembership> listed =
                await memberships.ListActiveMembershipsByUserIdAsync(
                    TestData.Users.ViewerAId);

            Assert.DoesNotContain(
                listed,
                item => item.OrganizationId == TestData.OrganizationA.Id);
        }
        finally
        {
            await using SqlConnection connection =
                await _fixture.OpenConnectionAsync(TestData.OrganizationA.Id);
            await connection.ExecuteAsync(
                """
                UPDATE [org].[Role]
                SET [IsActive] = 1, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @RoleId;
                """,
                new { RoleId = TestData.OrganizationA.ViewerRoleId });
        }
    }

    [Fact]
    public async Task Deleting_last_staff_role_excludes_membership_from_list()
    {
        Guid membershipId = await GetStaffMembershipIdAsync(
            TestData.OrganizationA.Id,
            TestData.Users.ViewerAId);

        List<StaffRoleRow> backup;

        await using (SqlConnection connection =
                         await _fixture.OpenConnectionAsync(TestData.OrganizationA.Id))
        {
            backup = (await connection.QueryAsync<StaffRoleRow>(
                """
                SELECT [Id], [OrganizationId], [StaffMembershipId], [RoleId],
                       [CreatedAt], [UpdatedAt]
                FROM [org].[StaffRole]
                WHERE [StaffMembershipId] = @MembershipId;
                """,
                new { MembershipId = membershipId })).ToList();

            await connection.ExecuteAsync(
                """
                DELETE FROM [org].[StaffRole]
                WHERE [StaffMembershipId] = @MembershipId;
                """,
                new { MembershipId = membershipId });
        }

        try
        {
            await using TenantScope unbound = TenantScope.Unbound(_fixture);
            await using AsyncServiceScope scope = unbound.BeginUnitOfWork();

            IStaffMembershipRepository memberships =
                scope.ServiceProvider.GetRequiredService<IStaffMembershipRepository>();

            IReadOnlyList<ActiveOrganizationMembership> listed =
                await memberships.ListActiveMembershipsByUserIdAsync(
                    TestData.Users.ViewerAId);

            Assert.DoesNotContain(
                listed,
                item => item.OrganizationId == TestData.OrganizationA.Id);
        }
        finally
        {
            await using SqlConnection connection =
                await _fixture.OpenConnectionAsync(TestData.OrganizationA.Id);

            foreach (StaffRoleRow row in backup)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO [org].[StaffRole]
                        ([Id], [OrganizationId], [StaffMembershipId], [RoleId],
                         [CreatedAt], [UpdatedAt])
                    VALUES
                        (@Id, @OrganizationId, @StaffMembershipId, @RoleId,
                         @CreatedAt, @UpdatedAt);
                    """,
                    row);
            }
        }
    }

    [Fact]
    public async Task Multi_role_membership_remains_listed_when_one_role_is_inactive()
    {
        Guid membershipId = await GetStaffMembershipIdAsync(
            TestData.OrganizationA.Id,
            TestData.Users.AdministratorAId);

        Guid extraRoleId = Guid.CreateVersion7();
        Guid extraStaffRoleId = Guid.CreateVersion7();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using (SqlConnection connection =
                         await _fixture.OpenConnectionAsync(TestData.OrganizationA.Id))
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO [org].[Role]
                    ([Id], [OrganizationId], [Name], [Key], [IsSystemRole],
                     [IsActive], [CreatedAt], [UpdatedAt])
                VALUES
                    (@RoleId, @OrganizationId, N'Extra', N'extra_role', 0,
                     0, @Now, @Now);

                INSERT INTO [org].[StaffRole]
                    ([Id], [OrganizationId], [StaffMembershipId], [RoleId],
                     [CreatedAt], [UpdatedAt])
                VALUES
                    (@StaffRoleId, @OrganizationId, @MembershipId, @RoleId, @Now, @Now);
                """,
                new
                {
                    RoleId = extraRoleId,
                    OrganizationId = TestData.OrganizationA.Id,
                    StaffRoleId = extraStaffRoleId,
                    MembershipId = membershipId,
                    Now = now
                });
        }

        try
        {
            await using TenantScope unbound = TenantScope.Unbound(_fixture);
            await using AsyncServiceScope scope = unbound.BeginUnitOfWork();

            IStaffMembershipRepository memberships =
                scope.ServiceProvider.GetRequiredService<IStaffMembershipRepository>();

            IReadOnlyList<ActiveOrganizationMembership> listed =
                await memberships.ListActiveMembershipsByUserIdAsync(
                    TestData.Users.AdministratorAId);

            Assert.Contains(
                listed,
                item => item.OrganizationId == TestData.OrganizationA.Id
                    && item.StaffMembershipId == membershipId);
        }
        finally
        {
            await using SqlConnection connection =
                await _fixture.OpenConnectionAsync(TestData.OrganizationA.Id);
            await connection.ExecuteAsync(
                """
                DELETE FROM [org].[StaffRole] WHERE [Id] = @StaffRoleId;
                DELETE FROM [org].[Role] WHERE [Id] = @RoleId;
                """,
                new { StaffRoleId = extraStaffRoleId, RoleId = extraRoleId });
        }
    }

    private async Task<Guid> GetStaffMembershipIdAsync(Guid organizationId, Guid userId)
    {
        await using SqlConnection connection =
            await _fixture.OpenConnectionAsync(organizationId);

        return await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT [Id]
            FROM [org].[StaffMembership]
            WHERE [OrganizationId] = @OrganizationId
              AND [UserId] = @UserId
              AND [DeletedAt] IS NULL;
            """,
            new { OrganizationId = organizationId, UserId = userId });
    }

    private sealed class StaffRoleRow
    {
        public Guid Id { get; init; }

        public Guid OrganizationId { get; init; }

        public Guid StaffMembershipId { get; init; }

        public Guid RoleId { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }
    }
}
