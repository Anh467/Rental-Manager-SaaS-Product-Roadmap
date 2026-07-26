using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Authorization;
using RentalManager.Api.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.GlobalFields;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;

namespace RentalManager.Api.Controllers;

[ApiController, Authorize, Route("api/v1/global/fields")]
public sealed class GlobalFieldsController(IGlobalFieldService fields) : ControllerBase
{
    [HttpGet, RequiresPermission(GlobalFieldPermissions.View)]
    public async Task<ActionResult<ApiResponse<ApiPageResult<FieldDto>>>> List(
        [FromQuery] GetGlobalFieldsRequest request, CancellationToken ct) =>
        Ok(ApiResponse<ApiPageResult<FieldDto>>.Create(
            ApiPageResult<FieldDto>.From(await fields.GetFieldsAsync(request, ct)),
            MessageCode.Success.Retrieved, correlationId: HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}"), RequiresPermission(GlobalFieldPermissions.View)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> Get(Guid id, CancellationToken ct) =>
        Ok(ApiResponse<FieldDto>.Create(await fields.GetFieldAsync(id, ct), MessageCode.Success.Retrieved,
            correlationId: HttpContext.TraceIdentifier));

    [HttpPost, RequiresPermission(GlobalFieldPermissions.Add)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> Create(
        [FromBody] CreateFieldRequest request,
        CancellationToken ct)
    {
        var field = await fields.CreateFieldAsync(request, ct);
        return Created($"/api/v1/global/fields/{field.Id}", ApiResponse<FieldDto>.Create(field, MessageCode.Success.Created,
            correlationId: HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}"), RequiresPermission(GlobalFieldPermissions.Edit)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> Update(
        Guid id,
        [FromBody] UpdateFieldRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<FieldDto>.Create(await fields.UpdateFieldAsync(id, request, ct), MessageCode.Success.Updated,
            correlationId: HttpContext.TraceIdentifier));

    [HttpPatch("{id:guid}/status"), RequiresPermission(GlobalFieldPermissions.Edit)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> Status(
        Guid id,
        [FromBody] UpdateGlobalFieldStatusRequest request,
        CancellationToken ct) =>
        Ok(ApiResponse<FieldDto>.Create(await fields.UpdateStatusAsync(id, request, ct), MessageCode.Success.Updated,
            correlationId: HttpContext.TraceIdentifier));

    [HttpDelete("{id:guid}"), RequiresPermission(GlobalFieldPermissions.Delete)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid id,
        [FromBody] DeleteFieldRequest request,
        CancellationToken ct)
    {
        await fields.DeleteFieldAsync(id, request, ct);
        return Ok(ApiResponse<object>.Create(null, MessageCode.Success.Deactivated, correlationId: HttpContext.TraceIdentifier));
    }
}
