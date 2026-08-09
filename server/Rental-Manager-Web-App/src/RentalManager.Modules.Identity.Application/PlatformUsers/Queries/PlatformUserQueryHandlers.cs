using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.PlatformUsers.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.Identity.Application.PlatformUsers.Queries;

public sealed class GetPlatformUsersQueryHandler(IPlatformUserStore users)
    : IQueryHandler<GetPlatformUsersQuery, PagedResult<PlatformUserDto>>
{
    public async Task<PagedResult<PlatformUserDto>> HandleAsync(
        GetPlatformUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Request);

        GetPlatformUsersRequest request = query.Request;
        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        PlatformUserListResult result = await users.ListAsync(
            new PlatformUserListQuery(request.Search, request.IsActive, page, pageSize),
            cancellationToken);

        return new PagedResult<PlatformUserDto>(
            result.Items.Select(PlatformUserMapper.ToDto).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }
}

public sealed class GetPlatformUserQueryHandler(IPlatformUserStore users)
    : IQueryHandler<GetPlatformUserQuery, PlatformUserDto>
{
    public async Task<PlatformUserDto> HandleAsync(
        GetPlatformUserQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        PlatformUserRecord user = await users.GetByIdAsync(query.UserId, cancellationToken)
            ?? throw new ResourceNotFoundException(PlatformUserInvariants.ObjectName);

        bool isSelf = query.ActorUserId is Guid actor && actor == query.UserId;
        // Authorization for non-self is enforced by the controller permission
        // attribute / IAuthorizationService. Self-view is always allowed here.
        _ = isSelf;

        return PlatformUserMapper.ToDto(user);
    }
}
