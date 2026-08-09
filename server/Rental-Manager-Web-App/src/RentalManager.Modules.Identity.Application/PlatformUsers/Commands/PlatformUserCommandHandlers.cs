using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.PlatformUsers.Contracts;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.Identity.Application.PlatformUsers.Commands;

public sealed class UpdatePlatformUserCommandHandler(
    IPlatformUserStore users,
    ISecurityEventPublisher securityEvents,
    IOrganizationContext organizationContext)
    : ICommandHandler<UpdatePlatformUserCommand, PlatformUserDto>
{
    public async Task<PlatformUserDto> HandleAsync(
        UpdatePlatformUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        string displayName = command.Request.DisplayName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 256)
        {
            throw new ValidationFailedException(
                nameof(UpdatePlatformUserRequest.DisplayName),
                MessageCode.Error.ValidationFailed);
        }

        PlatformUserRecord existing = await users.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new ResourceNotFoundException(PlatformUserInvariants.ObjectName);

        byte[] rowVersion = PlatformUserMapper.ParseRowVersion(command.Request.RowVersion);

        PlatformUserRecord updated = await users.UpdateAsync(
            command.UserId,
            displayName,
            rowVersion,
            cancellationToken);

        await securityEvents.PublishAsync(
            SecurityEvent.Create(
                SecurityEventTypes.UserUpdated,
                organizationContext.CorrelationId,
                new Dictionary<string, object?>
                {
                    [SecurityEventFields.ActorUserId] = command.ActorUserId,
                    [SecurityEventFields.TargetUserId] = command.UserId,
                    [SecurityEventFields.Before] = existing.DisplayName,
                    [SecurityEventFields.After] = updated.DisplayName,
                    [SecurityEventFields.Result] = SecurityEventReasons.Succeeded
                }),
            cancellationToken);

        return PlatformUserMapper.ToDto(updated);
    }
}

public sealed class ActivatePlatformUserCommandHandler(
    IPlatformUserStore users,
    ISecurityEventPublisher securityEvents,
    IOrganizationContext organizationContext)
    : ICommandHandler<ActivatePlatformUserCommand, PlatformUserDto>
{
    public Task<PlatformUserDto> HandleAsync(
        ActivatePlatformUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        if (!command.Request.IsActive)
        {
            throw new ValidationFailedException(
                nameof(UpdatePlatformUserStatusRequest.IsActive),
                MessageCode.Error.ValidationFailed);
        }

        return PlatformUserStatusChange.ExecuteAsync(
            users,
            securityEvents,
            organizationContext,
            command.UserId,
            command.ActorUserId,
            isActive: true,
            command.Request.RowVersion,
            SecurityEventTypes.UserActivated,
            cancellationToken);
    }
}

public sealed class InactivatePlatformUserCommandHandler(
    IPlatformUserStore users,
    ISecurityEventPublisher securityEvents,
    IOrganizationContext organizationContext)
    : ICommandHandler<InactivatePlatformUserCommand, PlatformUserDto>
{
    public async Task<PlatformUserDto> HandleAsync(
        InactivatePlatformUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        if (command.Request.IsActive)
        {
            throw new ValidationFailedException(
                nameof(UpdatePlatformUserStatusRequest.IsActive),
                MessageCode.Error.ValidationFailed);
        }

        // Inactivate flips IsActive only; membership rows must remain.
        _ = await users.CountOrganizationMembershipsAsync(command.UserId, cancellationToken);

        return await PlatformUserStatusChange.ExecuteAsync(
            users,
            securityEvents,
            organizationContext,
            command.UserId,
            command.ActorUserId,
            isActive: false,
            command.Request.RowVersion,
            SecurityEventTypes.UserDeactivated,
            cancellationToken);
    }
}

internal static class PlatformUserStatusChange
{
    public static async Task<PlatformUserDto> ExecuteAsync(
        IPlatformUserStore users,
        ISecurityEventPublisher securityEvents,
        IOrganizationContext organizationContext,
        Guid userId,
        Guid? actorUserId,
        bool isActive,
        string? rowVersionText,
        string eventType,
        CancellationToken cancellationToken)
    {
        PlatformUserRecord existing = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException(PlatformUserInvariants.ObjectName);

        byte[] rowVersion = PlatformUserMapper.ParseRowVersion(rowVersionText);

        PlatformUserRecord updated = await users.SetActiveAsync(
            userId,
            isActive,
            rowVersion,
            cancellationToken);

        await securityEvents.PublishAsync(
            SecurityEvent.Create(
                eventType,
                organizationContext.CorrelationId,
                new Dictionary<string, object?>
                {
                    [SecurityEventFields.ActorUserId] = actorUserId,
                    [SecurityEventFields.TargetUserId] = userId,
                    [SecurityEventFields.Before] = existing.IsActive,
                    [SecurityEventFields.After] = updated.IsActive,
                    [SecurityEventFields.Result] = SecurityEventReasons.Succeeded
                }),
            cancellationToken);

        return PlatformUserMapper.ToDto(updated);
    }
}
