using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Modules.Identity.Infrastructure.Authorization;
using RentalManager.Api.Contracts;
using RentalManager.BuildingBlocks.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Fields.Commands;
using RentalManager.Modules.TenantManagement.Application.Fields.Queries;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

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

    private readonly IQueryHandler<GetFieldsQuery, PagedResult<FieldDto>> _getFields;
    private readonly IQueryHandler<GetFieldQuery, FieldDto> _getField;
    private readonly ICommandHandler<CreateFieldCommand, FieldDto> _createField;
    private readonly ICommandHandler<UpdateFieldCommand, FieldDto> _updateField;
    private readonly ICommandHandler<DeleteFieldCommand> _deleteField;

    public FieldsController(
        IQueryHandler<GetFieldsQuery, PagedResult<FieldDto>> getFields,
        IQueryHandler<GetFieldQuery, FieldDto> getField,
        ICommandHandler<CreateFieldCommand, FieldDto> createField,
        ICommandHandler<UpdateFieldCommand, FieldDto> updateField,
        ICommandHandler<DeleteFieldCommand> deleteField)
    {
        ArgumentNullException.ThrowIfNull(getFields);
        ArgumentNullException.ThrowIfNull(getField);
        ArgumentNullException.ThrowIfNull(createField);
        ArgumentNullException.ThrowIfNull(updateField);
        ArgumentNullException.ThrowIfNull(deleteField);

        _getFields = getFields;
        _getField = getField;
        _createField = createField;
        _updateField = updateField;
        _deleteField = deleteField;
    }

    [HttpGet]
    [RequiresPermission(FieldPermissions.View)]
    public async Task<ActionResult<ApiResponse<ApiPageResult<FieldDto>>>> GetFieldsAsync(
        [FromQuery] GetFieldsRequest request,
        CancellationToken cancellationToken)
    {
        PagedResult<FieldDto> page = await _getFields.HandleAsync(
            new GetFieldsQuery(request),
            cancellationToken);

        return Ok(ApiResponse<ApiPageResult<FieldDto>>.Create(
            page.ToApiPageResult(),
            MessageCode.Success.Retrieved,
            HttpContext.TraceIdentifier,
            FieldObjectParameters));
    }

    [HttpGet("{id:guid}")]
    [RequiresPermission(FieldPermissions.View)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> GetFieldAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        FieldDto field = await _getField.HandleAsync(
            new GetFieldQuery(id),
            cancellationToken);

        return Ok(ApiResponse<FieldDto>.Create(
            field,
            MessageCode.Success.Retrieved,
            HttpContext.TraceIdentifier,
            FieldObjectParameters));
    }

    [HttpPost]
    [RequiresPermission(FieldPermissions.Add)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> CreateFieldAsync(
        [FromBody] CreateFieldRequest request,
        CancellationToken cancellationToken)
    {
        FieldDto field = await _createField.HandleAsync(
            new CreateFieldCommand(request),
            cancellationToken);

        return Created(
            $"/api/v1/fields/{field.Id}",
            ApiResponse<FieldDto>.Create(
                field,
                MessageCode.Success.Created,
                HttpContext.TraceIdentifier,
                FieldObjectParameters));
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(FieldPermissions.Edit)]
    public async Task<ActionResult<ApiResponse<FieldDto>>> UpdateFieldAsync(
        Guid id,
        [FromBody] UpdateFieldRequest request,
        CancellationToken cancellationToken)
    {
        FieldDto field = await _updateField.HandleAsync(
            new UpdateFieldCommand(id, request),
            cancellationToken);

        return Ok(ApiResponse<FieldDto>.Create(
            field,
            MessageCode.Success.Updated,
            HttpContext.TraceIdentifier,
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
        await _deleteField.HandleAsync(
            new DeleteFieldCommand(id, request),
            cancellationToken);

        return Ok(ApiResponse<object>.Create(
            null,
            MessageCode.Success.Deactivated,
            HttpContext.TraceIdentifier,
            FieldObjectParameters));
    }
}
