using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Authorization;
using RentalManager.Api.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/fields")]
public sealed class FieldsController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, object?> FieldObjectParameters =
        new Dictionary<string, object?>
        {
            [MessageCode.Parameter.Object] = MessageCode.ObjectName.Field
        };

    private readonly IFieldService _fieldService;

    public FieldsController(IFieldService fieldService)
    {
        ArgumentNullException.ThrowIfNull(fieldService);
        _fieldService = fieldService;
    }

    [HttpGet]
    [RequiresPermission(FieldPermissions.View)]
    public async Task<ActionResult<ApiResponse<ApiPageResult<FieldDto>>>> GetFieldsAsync(
        [FromQuery] GetFieldsRequest request,
        CancellationToken cancellationToken)
    {
        PagedResult<FieldDto> page = await _fieldService.GetFieldsAsync(
            request,
            cancellationToken);

        return Ok(ApiResponse<ApiPageResult<FieldDto>>.Create(
            ApiPageResult<FieldDto>.From(page),
            MessageCode.Success.Retrieved,
            FieldObjectParameters));
    }

    [HttpGet("{id:guid}")]
    [RequiresPermission(FieldPermissions.View)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> GetFieldAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        FieldDto field = await _fieldService.GetFieldAsync(id, cancellationToken);

        return Ok(ApiResponse<FieldDto>.Create(
            field,
            MessageCode.Success.Retrieved,
            FieldObjectParameters));
    }

    [HttpPost]
    [RequiresPermission(FieldPermissions.Add)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> CreateFieldAsync(
        [FromBody] CreateFieldRequest request,
        CancellationToken cancellationToken)
    {
        FieldDto field = await _fieldService.CreateFieldAsync(request, cancellationToken);

        return Created(
            $"/api/v1/fields/{field.Id}",
            ApiResponse<FieldDto>.Create(
                field,
                MessageCode.Success.Created,
                FieldObjectParameters));
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(FieldPermissions.Edit)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> UpdateFieldAsync(
        Guid id,
        [FromBody] UpdateFieldRequest request,
        CancellationToken cancellationToken)
    {
        FieldDto field = await _fieldService.UpdateFieldAsync(
            id,
            request,
            cancellationToken);

        return Ok(ApiResponse<FieldDto>.Create(
            field,
            MessageCode.Success.Updated,
            FieldObjectParameters));
    }

    /// <summary>
    /// Soft deletes a field. Returns 200 with a body rather than 204 because the
    /// response envelope carries the message key the client shows to the user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequiresPermission(FieldPermissions.Delete)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteFieldAsync(
        Guid id,
        [FromBody] DeleteFieldRequest request,
        CancellationToken cancellationToken)
    {
        await _fieldService.DeleteFieldAsync(id, request, cancellationToken);

        return Ok(ApiResponse<object>.Create(
            null,
            MessageCode.Success.Deactivated,
            FieldObjectParameters));
    }
}
