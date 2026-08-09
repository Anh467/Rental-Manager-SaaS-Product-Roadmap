using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Missing_user_context_fails_closed_without_succeeding()
    {
        var handler = new PermissionAuthorizationHandler(
            new ThrowingPermissionReader(),
            new EmptyPlatformPermissionReader(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        var requirement = new PermissionRequirement("field_view");
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Infrastructure_exception_from_permission_reader_propagates()
    {
        Guid userId = Guid.CreateVersion7();
        Guid organizationId = Guid.CreateVersion7();

        var handler = new PermissionAuthorizationHandler(
            new ThrowingPermissionReader(),
            new EmptyPlatformPermissionReader(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        var requirement = new PermissionRequirement("field_view");
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(IdentityClaimNames.UserId, userId.ToString()),
                    new Claim(IdentityClaimNames.ActiveOrganizationId, organizationId.ToString()),
                    new Claim(IdentityClaimNames.Scope, IdentityClaimNames.ScopeOrganization)
                ],
                authenticationType: "test")),
            resource: null);

        IOException exception = await Assert.ThrowsAsync<IOException>(
            () => handler.HandleAsync(context));
        Assert.Equal("simulated database outage", exception.Message);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Global_scope_uses_platform_permissions_only()
    {
        Guid userId = Guid.CreateVersion7();
        var platform = new FixedPlatformPermissionReader(["platform.users.view"]);

        var handler = new PermissionAuthorizationHandler(
            new ThrowingPermissionReader(),
            platform,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        var requirement = new PermissionRequirement("platform.users.view");
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(IdentityClaimNames.UserId, userId.ToString()),
                    new Claim(IdentityClaimNames.Scope, IdentityClaimNames.ScopeGlobal)
                ],
                authenticationType: "test")),
            resource: null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.Equal(1, platform.GetPermissionCalls);
    }

    [Fact]
    public async Task Global_scope_without_platform_permission_fails_closed()
    {
        Guid userId = Guid.CreateVersion7();

        var handler = new PermissionAuthorizationHandler(
            new ThrowingPermissionReader(),
            new EmptyPlatformPermissionReader(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        var requirement = new PermissionRequirement("platform.users.view");
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(IdentityClaimNames.UserId, userId.ToString()),
                    new Claim(IdentityClaimNames.Scope, IdentityClaimNames.ScopeGlobal)
                ],
                authenticationType: "test")),
            resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private sealed class EmptyPlatformPermissionReader : IPlatformPermissionReader
    {
        public Task<bool> HasAnyPlatformRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlySet<string>> GetPermissionKeysAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));

        public Task<PlatformRoleSummary?> GetPrimaryRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformRoleSummary?>(null);
    }

    private sealed class FixedPlatformPermissionReader(IEnumerable<string> keys)
        : IPlatformPermissionReader
    {
        private readonly HashSet<string> _keys = new(keys, StringComparer.Ordinal);

        public int GetPermissionCalls { get; private set; }

        public Task<bool> HasAnyPlatformRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_keys.Count > 0);

        public Task<IReadOnlySet<string>> GetPermissionKeysAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            GetPermissionCalls++;
            return Task.FromResult<IReadOnlySet<string>>(_keys);
        }

        public Task<PlatformRoleSummary?> GetPrimaryRoleAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformRoleSummary?>(
                _keys.Count == 0
                    ? null
                    : new PlatformRoleSummary("PLATFORM_SUPER_ADMIN", "Platform Super Admin"));
    }

    private sealed class ThrowingPermissionReader : IPermissionReader
    {
        public Task<bool> IsActiveMemberAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new IOException("simulated database outage");

        public Task<IReadOnlySet<string>> GetPermissionKeysAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new IOException("simulated database outage");
    }
}
