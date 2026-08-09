using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.PlatformUsers.Contracts;

namespace RentalManager.Modules.Identity.Application.PlatformUsers.Commands;

public sealed record UpdatePlatformUserCommand(
    Guid UserId,
    UpdatePlatformUserRequest Request,
    Guid? ActorUserId) : ICommand<PlatformUserDto>;

public sealed record ActivatePlatformUserCommand(
    Guid UserId,
    UpdatePlatformUserStatusRequest Request,
    Guid? ActorUserId) : ICommand<PlatformUserDto>;

public sealed record InactivatePlatformUserCommand(
    Guid UserId,
    UpdatePlatformUserStatusRequest Request,
    Guid? ActorUserId) : ICommand<PlatformUserDto>;
