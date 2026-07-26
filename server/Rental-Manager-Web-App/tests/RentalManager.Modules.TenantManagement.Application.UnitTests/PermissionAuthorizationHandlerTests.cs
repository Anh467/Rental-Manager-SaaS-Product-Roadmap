using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RentalManager.Api.Authorization;
using RentalManager.Api.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Missing_user_context_fails_closed_without_succeeding()
    {
        var handler = new PermissionAuthorizationHandler(
            new ThrowingPermissionReader(),
            new StubOrganizationContext(organizationId: null, userId: null),
            new UnusedUserRepository(),
            new UnusedRolePermissionRepository(),
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
        var handler = new PermissionAuthorizationHandler(
            new ThrowingPermissionReader(),
            new StubOrganizationContext(
                organizationId: Guid.CreateVersion7(),
                userId: Guid.CreateVersion7()),
            new UnusedUserRepository(),
            new UnusedRolePermissionRepository(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        var requirement = new PermissionRequirement("field_view");
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(JwtClaimNames.Scope, "organization")],
                authenticationType: "test")),
            resource: null);

        IOException exception = await Assert.ThrowsAsync<IOException>(
            () => handler.HandleAsync(context));
        Assert.Equal("simulated database outage", exception.Message);
        Assert.False(context.HasSucceeded);
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

    private sealed class StubOrganizationContext : IOrganizationContext
    {
        public StubOrganizationContext(Guid? organizationId, Guid? userId)
        {
            OrganizationId = organizationId;
            UserId = userId;
        }

        public Guid? OrganizationId { get; }

        public Guid? UserId { get; }

        public string? CorrelationId => "test";

        public bool HasOrganization => OrganizationId is not null;
    }

    private sealed class UnusedUserRepository : IUserRepository
    {
        public Task<User?> FindByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<User>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<byte[]?> InsertAsync(
            User entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<byte[]?> UpdateAsync(
            User entity,
            byte[]? expectedRowVersion = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            User entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            User entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SoftDeleteAsync(
            Guid id,
            byte[]? expectedRowVersion = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class UnusedRolePermissionRepository : IRolePermissionRepository
    {
        public Task<bool> ExistsAsync(
            RolePermission entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            RolePermission entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            RolePermission entity,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlySet<string>> GetPermissionKeysByRoleAsync(
            Guid roleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
