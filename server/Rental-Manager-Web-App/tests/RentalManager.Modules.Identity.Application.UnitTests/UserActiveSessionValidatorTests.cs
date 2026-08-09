using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.Identity.Infrastructure.Authentication;
using RentalManager.Modules.Identity.Infrastructure.Sessions;
using Xunit;

namespace RentalManager.Modules.Identity.Application.UnitTests;

public sealed class UserActiveSessionValidatorTests
{
    [Fact]
    public async Task Rejects_inactive_user_session()
    {
        Guid userId = Guid.CreateVersion7();
        string stamp = Guid.NewGuid().ToString("N");

        CookieValidatePrincipalContext context = await ValidateAsync(
            userId,
            stamp,
            new UserSessionState(userId, IsActive: false, stamp, DeletedAt: null));

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task Rejects_security_stamp_mismatch_after_inactivate()
    {
        Guid userId = Guid.CreateVersion7();

        CookieValidatePrincipalContext context = await ValidateAsync(
            userId,
            cookieStamp: "old-stamp",
            new UserSessionState(userId, IsActive: true, SecurityStamp: "new-stamp", DeletedAt: null));

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task Accepts_active_user_with_matching_stamp()
    {
        Guid userId = Guid.CreateVersion7();
        string stamp = Guid.NewGuid().ToString("N");

        CookieValidatePrincipalContext context = await ValidateAsync(
            userId,
            stamp,
            new UserSessionState(userId, IsActive: true, stamp, DeletedAt: null));

        Assert.NotNull(context.Principal);
        Assert.True(context.Principal!.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task Rejects_organization_session_after_membership_inactive()
    {
        Guid userId = Guid.CreateVersion7();
        Guid organizationId = Guid.CreateVersion7();
        Guid staffMembershipId = Guid.CreateVersion7();
        string stamp = Guid.NewGuid().ToString("N");

        CookieValidatePrincipalContext context = await ValidateAsync(
            userId,
            stamp,
            new UserSessionState(userId, IsActive: true, stamp, DeletedAt: null),
            organizationId,
            staffMembershipId,
            activeMembershipId: null);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task Accepts_organization_session_with_matching_active_membership()
    {
        Guid userId = Guid.CreateVersion7();
        Guid organizationId = Guid.CreateVersion7();
        Guid staffMembershipId = Guid.CreateVersion7();
        string stamp = Guid.NewGuid().ToString("N");

        CookieValidatePrincipalContext context = await ValidateAsync(
            userId,
            stamp,
            new UserSessionState(userId, IsActive: true, stamp, DeletedAt: null),
            organizationId,
            staffMembershipId,
            activeMembershipId: staffMembershipId);

        Assert.NotNull(context.Principal);
        Assert.True(context.Principal!.Identity!.IsAuthenticated);
    }

    private static async Task<CookieValidatePrincipalContext> ValidateAsync(
        Guid userId,
        string cookieStamp,
        UserSessionState? state,
        Guid? organizationId = null,
        Guid? claimStaffMembershipId = null,
        Guid? activeMembershipId = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(IdentityAuthenticationSchemes.ApplicationCookie)
            .AddCookie(IdentityAuthenticationSchemes.ApplicationCookie);
        services.AddSingleton<IUserSessionStateReader>(new FakeSessionStateReader(state));
        services.AddSingleton<IOrganizationMembershipReader>(
            new FakeMembershipReader(organizationId, activeMembershipId));
        services.AddSingleton<ISecurityEventPublisher, NoopSecurityEventPublisher>();
        ServiceProvider provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };

        var claims = new List<Claim>
        {
            new(IdentityClaimNames.UserId, userId.ToString()),
            new(IdentityClaimNames.SecurityStamp, cookieStamp)
        };

        if (organizationId is Guid orgId)
        {
            claims.Add(new Claim(IdentityClaimNames.ActiveOrganizationId, orgId.ToString()));
            claims.Add(new Claim(IdentityClaimNames.Scope, IdentityClaimNames.ScopeOrganization));
        }

        if (claimStaffMembershipId is Guid membershipId)
        {
            claims.Add(new Claim(IdentityClaimNames.StaffMembershipId, membershipId.ToString()));
        }

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: IdentityAuthenticationSchemes.ApplicationCookie);

        var scheme = new AuthenticationScheme(
            IdentityAuthenticationSchemes.ApplicationCookie,
            displayName: null,
            typeof(CookieAuthenticationHandler));

        var options = new CookieAuthenticationOptions();
        var context = new CookieValidatePrincipalContext(
            httpContext,
            new AuthenticationScheme(
                IdentityAuthenticationSchemes.ApplicationCookie,
                displayName: null,
                typeof(CookieAuthenticationHandler)),
            options,
            new AuthenticationTicket(new ClaimsPrincipal(identity), scheme.Name));

        await UserActiveSessionValidator.ValidateAsync(context);
        return context;
    }

    private sealed class FakeSessionStateReader(UserSessionState? state) : IUserSessionStateReader
    {
        public Task<UserSessionState?> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(state);
    }

    private sealed class FakeMembershipReader(
        Guid? organizationId,
        Guid? activeMembershipId) : IOrganizationMembershipReader
    {
        public Task<IReadOnlyList<OrganizationOptionDto>> ListActiveOrganizationsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationOptionDto>>([]);

        public Task<bool> IsActiveMemberAsync(
            Guid userId,
            Guid requestedOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                organizationId == requestedOrganizationId && activeMembershipId is not null);

        public Task<Guid?> GetActiveStaffMembershipIdAsync(
            Guid userId,
            Guid requestedOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                organizationId == requestedOrganizationId ? activeMembershipId : null);
    }

    private sealed class NoopSecurityEventPublisher : ISecurityEventPublisher
    {
        public Task PublishAsync(
            SecurityEvent securityEvent,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
