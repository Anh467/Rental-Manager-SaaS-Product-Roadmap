using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Contracts.Tenants;
using RentalManager.Modules.TenantManagement.Application.Tenants.ActivateTenant;
using RentalManager.Modules.TenantManagement.Application.Tenants.CreateTenant;
using RentalManager.Modules.TenantManagement.Application.Tenants.GetTenantById;
using RentalManager.Modules.TenantManagement.Application.Tenants.SearchTenants;
using RentalManager.Modules.TenantManagement.Application.Tenants.SuspendTenant;
using RentalManager.Modules.TenantManagement.Application.Tenants.UpdateTenant;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Messaging;
using RentalManager.Modules.TenantManagement.Core.Contracts.Tenants;

namespace RentalManager.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateTenantResult>> CreateAsync(
        CreateTenantRequest request,
        [FromServices] ICommandHandler<CreateTenantCommand, CreateTenantResult> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateTenantCommand(request.Code, request.Name);
        CreateTenantResult result = await handler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { tenantId = result.TenantId },
            result);
    }

    [HttpPut("{tenantId:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid tenantId,
        UpdateTenantRequest request,
        [FromServices] ICommandHandler<UpdateTenantCommand> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new UpdateTenantCommand(tenantId, request.Name),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{tenantId:guid}/activate")]
    public async Task<IActionResult> ActivateAsync(
        Guid tenantId,
        [FromServices] ICommandHandler<ActivateTenantCommand> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ActivateTenantCommand(tenantId),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{tenantId:guid}/suspend")]
    public async Task<IActionResult> SuspendAsync(
        Guid tenantId,
        [FromServices] ICommandHandler<SuspendTenantCommand> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new SuspendTenantCommand(tenantId),
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{tenantId:guid}")]
    public async Task<ActionResult<TenantDetailsResponse>> GetByIdAsync(
        Guid tenantId,
        [FromServices] IQueryHandler<GetTenantByIdQuery, TenantDetailsResponse?> handler,
        CancellationToken cancellationToken)
    {
        TenantDetailsResponse? result = await handler.HandleAsync(
            new GetTenantByIdQuery(tenantId),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TenantListItem>>> SearchAsync(
        [FromQuery] string? keyword,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] IQueryHandler<SearchTenantsQuery, PagedResult<TenantListItem>> handler,
        CancellationToken cancellationToken)
    {
        PagedResult<TenantListItem> result = await handler.HandleAsync(
            new SearchTenantsQuery(keyword, page, pageSize),
            cancellationToken);

        return Ok(result);
    }
}
