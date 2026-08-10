using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.PlatformUsers;
using RentalManager.Modules.Identity.Application.PlatformUsers.Commands;
using RentalManager.Modules.Identity.Application.PlatformUsers.Contracts;
using RentalManager.Modules.Identity.Application.PlatformUsers.Queries;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using Xunit;

namespace RentalManager.Modules.Identity.Application.UnitTests;

public sealed class PlatformUserLifecycleTests
{
    private static readonly byte[] RowVersionV1 = [1, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] RowVersionV2 = [2, 0, 0, 0, 0, 0, 0, 0];

    [Fact]
    public async Task Self_view_returns_user_without_platform_permission_check_in_handler()
    {
        Guid userId = Guid.CreateVersion7();
        var store = new FakePlatformUserStore();
        store.Users[userId] = CreateUser(userId, isActive: true);

        var handler = new GetPlatformUserQueryHandler(store);
        PlatformUserDto dto = await handler.HandleAsync(
            new GetPlatformUserQuery(userId, ActorUserId: userId));

        Assert.Equal(userId, dto.Id);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task Activate_publishes_safe_audit_event_and_returns_SCS_004_payload()
    {
        Guid actorId = Guid.CreateVersion7();
        Guid targetId = Guid.CreateVersion7();
        var store = new FakePlatformUserStore();
        store.Users[targetId] = CreateUser(targetId, isActive: false);
        var events = new RecordingSecurityEventPublisher();

        var handler = new ActivatePlatformUserCommandHandler(
            store,
            events,
            new FakeOrganizationContext());

        PlatformUserDto dto = await handler.HandleAsync(
            new ActivatePlatformUserCommand(
                targetId,
                new UpdatePlatformUserStatusRequest
                {
                    IsActive = true,
                    RowVersion = Convert.ToBase64String(RowVersionV1)
                },
                actorId));

        Assert.True(dto.IsActive);
        Assert.Contains(
            events.Events,
            e => e.EventType == SecurityEventTypes.UserActivated
                && Equals(e.Data[SecurityEventFields.ActorUserId], actorId)
                && Equals(e.Data[SecurityEventFields.TargetUserId], targetId)
                && Equals(e.Data[SecurityEventFields.Before], false)
                && Equals(e.Data[SecurityEventFields.After], true)
                && Equals(e.Data[SecurityEventFields.Result], SecurityEventReasons.Succeeded));
        Assert.DoesNotContain(events.Events, ContainsSecret);
    }

    [Fact]
    public async Task Inactivate_rotates_security_stamp_keeps_memberships_and_publishes_audit()
    {
        Guid actorId = Guid.CreateVersion7();
        Guid targetId = Guid.CreateVersion7();
        var store = new FakePlatformUserStore
        {
            MembershipCount = 3
        };
        PlatformUserRecord original = CreateUser(targetId, isActive: true);
        store.Users[targetId] = original;
        store.SecurityStamps[targetId] = Guid.NewGuid().ToString("N");
        string originalStamp = store.SecurityStamps[targetId];
        var events = new RecordingSecurityEventPublisher();

        var handler = new InactivatePlatformUserCommandHandler(
            store,
            events,
            new FakeOrganizationContext());

        PlatformUserDto dto = await handler.HandleAsync(
            new InactivatePlatformUserCommand(
                targetId,
                new UpdatePlatformUserStatusRequest
                {
                    IsActive = false,
                    RowVersion = Convert.ToBase64String(RowVersionV1)
                },
                actorId));

        Assert.False(dto.IsActive);
        Assert.Equal(3, store.MembershipCount);
        Assert.NotEqual(originalStamp, store.SecurityStamps[targetId]);
        Assert.Contains(
            events.Events,
            e => e.EventType == SecurityEventTypes.UserDeactivated
                && Equals(e.Data[SecurityEventFields.ActorUserId], actorId)
                && Equals(e.Data[SecurityEventFields.TargetUserId], targetId));
        Assert.DoesNotContain(events.Events, ContainsSecret);
    }

    [Fact]
    public async Task Concurrent_update_throws_ERR_010()
    {
        Guid targetId = Guid.CreateVersion7();
        var store = new FakePlatformUserStore();
        store.Users[targetId] = CreateUser(targetId, isActive: true);
        var events = new RecordingSecurityEventPublisher();

        var handler = new InactivatePlatformUserCommandHandler(
            store,
            events,
            new FakeOrganizationContext());

        ConcurrencyConflictException exception =
            await Assert.ThrowsAsync<ConcurrencyConflictException>(
                () => handler.HandleAsync(
                    new InactivatePlatformUserCommand(
                        targetId,
                        new UpdatePlatformUserStatusRequest
                        {
                            IsActive = false,
                            RowVersion = Convert.ToBase64String(RowVersionV2)
                        },
                        Guid.CreateVersion7())));

        Assert.Equal(MessageCode.Error.ConcurrencyConflict, exception.MessageKey);
        Assert.True(store.Users[targetId].IsActive);
    }

    [Fact]
    public async Task Update_publishes_user_updated_event()
    {
        Guid targetId = Guid.CreateVersion7();
        var store = new FakePlatformUserStore();
        store.Users[targetId] = CreateUser(targetId, isActive: true);
        var events = new RecordingSecurityEventPublisher();

        var handler = new UpdatePlatformUserCommandHandler(
            store,
            events,
            new FakeOrganizationContext());

        PlatformUserDto dto = await handler.HandleAsync(
            new UpdatePlatformUserCommand(
                targetId,
                new UpdatePlatformUserRequest
                {
                    DisplayName = "Renamed",
                    RowVersion = Convert.ToBase64String(RowVersionV1)
                },
                Guid.CreateVersion7()));

        Assert.Equal("Renamed", dto.DisplayName);
        Assert.Contains(events.Events, e => e.EventType == SecurityEventTypes.UserUpdated);
    }

    [Fact]
    public async Task Missing_user_throws_ERR_002()
    {
        var handler = new GetPlatformUserQueryHandler(new FakePlatformUserStore());

        ResourceNotFoundException exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.HandleAsync(new GetPlatformUserQuery(Guid.CreateVersion7(), null)));

        Assert.Equal(MessageCode.Error.NotFound, exception.MessageKey);
    }

    [Fact]
    public void Platform_user_permissions_match_confluence_keys()
    {
        Assert.Equal("platform.user.view", PlatformUserPermissions.View);
        Assert.Equal("platform.user.edit", PlatformUserPermissions.Edit);
        Assert.Equal("platform.user.deactivate", PlatformUserPermissions.Deactivate);
    }

    [Fact]
    public void Hard_delete_endpoint_is_not_part_of_lifecycle_commands()
    {
        Assert.Null(
            typeof(InactivatePlatformUserCommand).Assembly.GetType(
                "RentalManager.Modules.Identity.Application.PlatformUsers.Commands.DeletePlatformUserCommand"));
    }

    private static PlatformUserRecord CreateUser(Guid id, bool isActive) =>
        new(
            id,
            "user@example.com",
            "User",
            isActive,
            GlobalRoleId: null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            RowVersionV1.ToArray());

    private static bool ContainsSecret(SecurityEvent securityEvent) =>
        securityEvent.Data.Values.Any(value =>
            value is string text
            && (text.Contains("password", StringComparison.OrdinalIgnoreCase)
                || text.Contains("secret", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)));

    private sealed class FakePlatformUserStore : IPlatformUserStore
    {
        public Dictionary<Guid, PlatformUserRecord> Users { get; } = new();

        public Dictionary<Guid, string> SecurityStamps { get; } = new();

        public int MembershipCount { get; set; }

        public Task<PlatformUserRecord?> GetByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.TryGetValue(userId, out PlatformUserRecord? user) ? user : null);

        public Task<PlatformUserListResult> ListAsync(
            PlatformUserListQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlatformUserListResult(Users.Values.ToArray(), Users.Count, 1, 20));

        public Task<PlatformUserRecord> UpdateAsync(
            Guid userId,
            string displayName,
            byte[] expectedRowVersion,
            CancellationToken cancellationToken = default)
        {
            PlatformUserRecord current = RequireMatch(userId, expectedRowVersion);
            var updated = current with
            {
                DisplayName = displayName,
                UpdatedAt = DateTimeOffset.UtcNow,
                RowVersion = RowVersionV2.ToArray()
            };
            Users[userId] = updated;
            return Task.FromResult(updated);
        }

        public Task<PlatformUserRecord> SetActiveAsync(
            Guid userId,
            bool isActive,
            byte[] expectedRowVersion,
            CancellationToken cancellationToken = default)
        {
            PlatformUserRecord current = RequireMatch(userId, expectedRowVersion);
            if (!isActive)
            {
                SecurityStamps[userId] = Guid.NewGuid().ToString("N");
            }

            var updated = current with
            {
                IsActive = isActive,
                UpdatedAt = DateTimeOffset.UtcNow,
                RowVersion = RowVersionV2.ToArray()
            };
            Users[userId] = updated;
            return Task.FromResult(updated);
        }

        public Task<int> CountOrganizationMembershipsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(MembershipCount);

        public Task AssignPlatformRoleAsync(
            Guid userId,
            Guid platformRoleId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        private PlatformUserRecord RequireMatch(Guid userId, byte[] expectedRowVersion)
        {
            if (!Users.TryGetValue(userId, out PlatformUserRecord? current))
            {
                throw new ResourceNotFoundException(PlatformUserInvariants.ObjectName);
            }

            if (!current.RowVersion.SequenceEqual(expectedRowVersion))
            {
                throw new ConcurrencyConflictException(PlatformUserInvariants.ObjectName);
            }

            if (!SecurityStamps.ContainsKey(userId))
            {
                SecurityStamps[userId] = Guid.NewGuid().ToString("N");
            }

            return current;
        }
    }

    private sealed class FakeOrganizationContext : IOrganizationContext
    {
        public Guid? OrganizationId => null;

        public Guid? UserId => null;

        public Guid? StaffMembershipId => null;

        public string? CorrelationId => "test-correlation";

        public bool HasOrganization => false;
    }

    private sealed class RecordingSecurityEventPublisher : ISecurityEventPublisher
    {
        public List<SecurityEvent> Events { get; } = [];

        public Task PublishAsync(
            SecurityEvent securityEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(securityEvent);
            return Task.CompletedTask;
        }
    }
}
