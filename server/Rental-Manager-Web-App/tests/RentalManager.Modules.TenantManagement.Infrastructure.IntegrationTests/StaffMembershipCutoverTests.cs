using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Upgrade cutover from residual OrganizationUser rows into StaffMembership +
/// StaffRole. Uses the named SCRUM-81 cutover script against a fixture database
/// that already has the DACPAC model (including residual OrganizationUser).
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class StaffMembershipCutoverTests
{
    private readonly SqlServerFixture _fixture;

    public StaffMembershipCutoverTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Cutover_script_migrates_organization_user_rows_idempotently()
    {
        Guid organizationId = TestData.OrganizationA.Id;
        Guid userId = Guid.CreateVersion7();
        Guid roleId = TestData.OrganizationA.AdministratorRoleId;
        Guid membershipSourceKey = Guid.CreateVersion7();

        await using (SqlConnection seed = await _fixture.OpenConnectionAsync(organizationId))
        {
            await seed.ExecuteAsync(
                """
                INSERT INTO [dbo].[User]
                    ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
                     [EmailConfirmed], [DisplayName], [SecurityStamp], [ConcurrencyStamp],
                     [LockoutEnabled], [AccessFailedCount], [CreatedAt], [UpdatedAt])
                VALUES
                    (@UserId, @Email, UPPER(@Email), @Email, UPPER(@Email), 1, N'Cutover User',
                     CONVERT(NVARCHAR(36), NEWID()), CONVERT(NVARCHAR(36), NEWID()),
                     0, 0, @Now, @Now);

                INSERT INTO [org].[OrganizationUser]
                    ([OrganizationId], [UserId], [RoleId], [IsActive], [CreatedAt], [UpdatedAt])
                VALUES
                    (@OrganizationId, @UserId, @RoleId, 1, @Now, @Now);
                """,
                new
                {
                    UserId = userId,
                    Email = $"cutover.{membershipSourceKey:N}@rentalmanager.test",
                    OrganizationId = organizationId,
                    RoleId = roleId,
                    Now = DateTimeOffset.UtcNow
                });
        }

        string cutoverSql = await File.ReadAllTextAsync(ResolveCutoverScriptPath());

        await using (SqlConnection connection = await _fixture.OpenConnectionAsync())
        {
            // sa bypasses RLS; cutover is an elevated upgrade operation.
            await connection.ExecuteAsync(cutoverSql);
            await connection.ExecuteAsync(cutoverSql);
        }

        await using SqlConnection verify = await _fixture.OpenConnectionAsync(organizationId);

        var membership = await verify.QuerySingleAsync<(Guid Id, byte Status)>(
            """
            SELECT [Id], [Status]
            FROM [org].[StaffMembership]
            WHERE [OrganizationId] = @OrganizationId
              AND [UserId] = @UserId
              AND [DeletedAt] IS NULL;
            """,
            new { OrganizationId = organizationId, UserId = userId });

        Assert.Equal((byte)2, membership.Status);

        Guid staffRoleId = await verify.ExecuteScalarAsync<Guid>(
            """
            SELECT [Id]
            FROM [org].[StaffRole]
            WHERE [StaffMembershipId] = @StaffMembershipId
              AND [RoleId] = @RoleId;
            """,
            new { StaffMembershipId = membership.Id, RoleId = roleId });

        Assert.NotEqual(Guid.Empty, staffRoleId);

        int duplicateMemberships = await verify.ExecuteScalarAsync<int>(
            """
            SELECT COUNT_BIG(1)
            FROM [org].[StaffMembership]
            WHERE [OrganizationId] = @OrganizationId
              AND [UserId] = @UserId
              AND [DeletedAt] IS NULL;
            """,
            new { OrganizationId = organizationId, UserId = userId });

        Assert.Equal(1, duplicateMemberships);
    }

    private static string ResolveCutoverScriptPath()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "src",
                "RentalManager.Database.SQLServer",
                "Scripts",
                "Cutover",
                "SCRUM-81-StaffMembership-Cutover.sql");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "SCRUM-81-StaffMembership-Cutover.sql was not found.");
    }
}
