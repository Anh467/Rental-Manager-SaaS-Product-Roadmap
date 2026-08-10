using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;
using RentalManager.Modules.TenantManagement.Infrastructure.Authorization;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class StaffMembershipPermissionReaderTests
{
    [Fact]
    public async Task Active_member_with_multiple_roles_unions_permissions()
    {
        var repository = new FakeStaffMembershipRepository
        {
            ActiveMembershipId = Guid.CreateVersion7(),
            PermissionKeys = ["field_view", "field_create", "field_view"]
        };

        var reader = new PermissionReader(repository);

        Assert.True(await reader.IsActiveMemberAsync(Guid.CreateVersion7()));

        IReadOnlySet<string> keys = await reader.GetPermissionKeysAsync(Guid.CreateVersion7());
        Assert.Equal(2, keys.Count);
        Assert.Contains("field_view", keys);
        Assert.Contains("field_create", keys);
    }

    [Fact]
    public async Task Inactive_membership_contributes_no_permissions()
    {
        var repository = new FakeStaffMembershipRepository
        {
            ActiveMembershipId = null,
            PermissionKeys = []
        };

        var reader = new PermissionReader(repository);

        Assert.False(await reader.IsActiveMemberAsync(Guid.CreateVersion7()));
        Assert.Empty(await reader.GetPermissionKeysAsync(Guid.CreateVersion7()));
    }
}

public sealed class OrganizationContextStaffMembershipTests
{
    [Fact]
    public void Trusted_context_carries_staff_membership_id()
    {
        var accessor = new OrganizationContextAccessor();
        Guid organizationId = Guid.CreateVersion7();
        Guid staffMembershipId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();

        accessor.SetUser(userId);
        accessor.SetOrganization(organizationId, staffMembershipId);
        accessor.SetCorrelationId("corr-1");

        Assert.Equal(userId, accessor.UserId);
        Assert.Equal(organizationId, accessor.OrganizationId);
        Assert.Equal(staffMembershipId, accessor.StaffMembershipId);
        Assert.Equal("corr-1", accessor.CorrelationId);
        Assert.True(accessor.HasOrganization);
    }

    [Fact]
    public void Explicit_context_exposes_staff_membership_id()
    {
        Guid organizationId = Guid.CreateVersion7();
        Guid staffMembershipId = Guid.CreateVersion7();

        var context = new ExplicitOrganizationContext(
            organizationId,
            userId: Guid.CreateVersion7(),
            correlationId: "corr-2",
            staffMembershipId: staffMembershipId);

        Assert.Equal(staffMembershipId, context.StaffMembershipId);
        Assert.True(context.HasOrganization);
    }
}

public sealed class NoRuntimeOrganizationUserReferencesTests
{
    [Fact]
    public void Source_and_test_projects_do_not_reference_OrganizationUser_at_runtime()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        DirectoryInfo? webAppRoot = null;

        while (directory is not null)
        {
            var candidate = new DirectoryInfo(Path.Combine(directory.FullName, "src"));
            if (candidate.Exists &&
                Directory.Exists(Path.Combine(candidate.FullName, "RentalManager.Api")))
            {
                webAppRoot = directory;
                break;
            }

            directory = directory.Parent;
        }

        Assert.NotNull(webAppRoot);

        string[] roots =
        [
            Path.Combine(webAppRoot!.FullName, "src"),
            Path.Combine(webAppRoot.FullName, "tests")
        ];

        var offenders = new List<string>();

        foreach (string root in roots)
        {
            foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                // Residual SQL table name may appear only in cutover/docs comments inside
                // .sql files; C# runtime must not mention the retired entity.
                string text = File.ReadAllText(file);
                if (text.Contains("OrganizationUser", StringComparison.Ordinal) ||
                    text.Contains("IOrganizationUserRepository", StringComparison.Ordinal))
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.Equals(
                            "NoRuntimeOrganizationUserReferencesTests.cs",
                            StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals(
                            "StaffMembershipCutoverTests.cs",
                            StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals(
                            "LegacySchemaUpgradeTests.cs",
                            StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals(
                            "StaffMembershipPermissionReaderTests.cs",
                            StringComparison.OrdinalIgnoreCase) ||
                        // Fixture cleanup for residual cutover rows only.
                        fileName.Equals(
                            "SqlServerFixture.cs",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    offenders.Add(Path.GetRelativePath(webAppRoot.FullName, file));
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Runtime OrganizationUser references remain in:" + Environment.NewLine +
            string.Join(Environment.NewLine, offenders));
    }
}

internal sealed class FakeStaffMembershipRepository : IStaffMembershipRepository
{
    public Guid? ActiveMembershipId { get; set; }

    public IReadOnlyCollection<string> PermissionKeys { get; set; } = [];

    public Task<StaffMembership?> FindActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            ActiveMembershipId is Guid id
                ? new StaffMembership
                {
                    Id = id,
                    UserId = userId,
                    Status = StaffMembershipStatuses.Active
                }
                : null);

    public Task<Guid?> GetActiveStaffMembershipIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveMembershipId);

    public Task<IReadOnlyList<ActiveOrganizationMembership>> ListActiveMembershipsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ActiveOrganizationMembership>>([]);

    public Task<IReadOnlyCollection<string>> GetPermissionKeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PermissionKeys);

    public Task SaveAsync(
        StaffMembership membership,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task AssignRoleAsync(
        Guid staffMembershipId,
        Guid roleId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SoftDeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
