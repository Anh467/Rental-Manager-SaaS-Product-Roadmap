using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.PlatformUsers.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

namespace RentalManager.Modules.Identity.Application.PlatformUsers.Queries;

public sealed record GetPlatformUsersQuery(GetPlatformUsersRequest Request)
    : IQuery<PagedResult<PlatformUserDto>>;

public sealed record GetPlatformUserQuery(Guid UserId, Guid? ActorUserId)
    : IQuery<PlatformUserDto>;
