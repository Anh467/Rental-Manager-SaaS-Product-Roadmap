using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Authorization;
using RentalManager.Api.Contracts;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Application.GlobalFields;
using RentalManager.Modules.TenantManagement.Core.Constants;

namespace RentalManager.Api.Controllers;

[ApiController, Authorize, Route("api/v1/global/field-types")]
public sealed class GlobalFieldTypesController(IGlobalFieldTypeService fieldTypes) : ControllerBase
{
    [HttpGet, RequiresPermission(GlobalFieldPermissions.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FieldTypeDto>>>> List(
        CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<FieldTypeDto>>.Create(
            await fieldTypes.GetFieldTypesAsync(ct),
            MessageCode.Success.Retrieved,
            correlationId: HttpContext.TraceIdentifier));
}
