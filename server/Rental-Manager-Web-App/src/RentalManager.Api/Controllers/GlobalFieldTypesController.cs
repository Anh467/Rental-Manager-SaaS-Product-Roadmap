using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Modules.Identity.Infrastructure.Authorization;
using RentalManager.BuildingBlocks.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.GlobalFields;
using RentalManager.Modules.TenantManagement.Application.GlobalFieldTypes.Queries;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Api.Controllers;

[ApiController, Authorize, Route("api/v1/global/field-types")]
public sealed class GlobalFieldTypesController(
    IQueryHandler<GetGlobalFieldTypesQuery, IReadOnlyList<FieldTypeDto>> getGlobalFieldTypes)
    : ControllerBase
{
    [HttpGet, RequiresPermission(GlobalFieldPermissions.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FieldTypeDto>>>> List(
        CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<FieldTypeDto>>.Create(
            await getGlobalFieldTypes.HandleAsync(new GetGlobalFieldTypesQuery(), ct),
            MessageCode.Success.Retrieved,
            correlationId: HttpContext.TraceIdentifier));
}
