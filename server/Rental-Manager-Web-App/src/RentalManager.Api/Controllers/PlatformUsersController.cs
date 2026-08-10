using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Contracts;
using RentalManager.BuildingBlocks.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.PlatformUsers;
using RentalManager.Modules.Identity.Application.PlatformUsers.Commands;
using RentalManager.Modules.Identity.Application.PlatformUsers.Contracts;
using RentalManager.Modules.Identity.Application.PlatformUsers.Queries;
using RentalManager.Modules.Identity.Infrastructure.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/platform/users")]
public sealed class PlatformUsersController(
    IQueryHandler<GetPlatformUsersQuery, PagedResult<PlatformUserDto>> listUsers,
    IQueryHandler<GetPlatformUserQuery, PlatformUserDto> getUser,
    ICommandHandler<UpdatePlatformUserCommand, PlatformUserDto> updateUser,
    ICommandHandler<ActivatePlatformUserCommand, PlatformUserDto> activateUser,
    ICommandHandler<InactivatePlatformUserCommand, PlatformUserDto> inactivateUser,
    ICurrentIdentity currentIdentity,
    IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PlatformUserPermissions.View)]
    public async Task<ActionResult<ApiResponse<ApiPageResult<PlatformUserDto>>>> List(
        [FromQuery] GetPlatformUsersRequest request,
        CancellationToken cancellationToken)
    {
        PagedResult<PlatformUserDto> page = await listUsers.HandleAsync(
            new GetPlatformUsersQuery(request),
            cancellationToken);

        return Ok(ApiResponse<ApiPageResult<PlatformUserDto>>.Create(
            page.ToApiPageResult(),
            MessageCode.Success.Retrieved,
            correlationId: HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PlatformUserDto>>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        await EnsureCanViewAsync(id);

        PlatformUserDto user = await getUser.HandleAsync(
            new GetPlatformUserQuery(id, currentIdentity.UserId),
            cancellationToken);

        return Ok(ApiResponse<PlatformUserDto>.Create(
            user,
            MessageCode.Success.Retrieved,
            correlationId: HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    [RequiresPermission(PlatformUserPermissions.Edit)]
    public async Task<ActionResult<ApiResponse<PlatformUserDto>>> Update(
        Guid id,
        [FromBody] UpdatePlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        PlatformUserDto user = await updateUser.HandleAsync(
            new UpdatePlatformUserCommand(id, request, currentIdentity.UserId),
            cancellationToken);

        return Ok(ApiResponse<PlatformUserDto>.Create(
            user,
            MessageCode.Success.Updated,
            correlationId: HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/activate")]
    [HttpPatch("{id:guid}/activate")]
    [RequiresPermission(PlatformUserPermissions.Edit)]
    public async Task<ActionResult<ApiResponse<PlatformUserDto>>> Activate(
        Guid id,
        [FromBody] UpdatePlatformUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        PlatformUserDto user = await activateUser.HandleAsync(
            new ActivatePlatformUserCommand(
                id,
                request with { IsActive = true },
                currentIdentity.UserId),
            cancellationToken);

        return Ok(ApiResponse<PlatformUserDto>.Create(
            user,
            MessageCode.Success.Reactivated,
            correlationId: HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/inactivate")]
    [HttpPatch("{id:guid}/inactivate")]
    [RequiresPermission(PlatformUserPermissions.Deactivate)]
    public async Task<ActionResult<ApiResponse<PlatformUserDto>>> Inactivate(
        Guid id,
        [FromBody] UpdatePlatformUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        PlatformUserDto user = await inactivateUser.HandleAsync(
            new InactivatePlatformUserCommand(
                id,
                request with { IsActive = false },
                currentIdentity.UserId),
            cancellationToken);

        return Ok(ApiResponse<PlatformUserDto>.Create(
            user,
            MessageCode.Success.Deactivated,
            correlationId: HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Self-view does not require platform.user.view; every other lookup does.
    /// </summary>
    private async Task EnsureCanViewAsync(Guid targetUserId)
    {
        if (currentIdentity.UserId is Guid actor && actor == targetUserId)
        {
            return;
        }

        AuthorizationResult authorized = await authorizationService.AuthorizeAsync(
            User,
            resource: null,
            policyName: PlatformUserPermissions.View);

        if (!authorized.Succeeded)
        {
            throw new PermissionDeniedException("view", PlatformUserInvariants.ObjectName);
        }
    }
}
