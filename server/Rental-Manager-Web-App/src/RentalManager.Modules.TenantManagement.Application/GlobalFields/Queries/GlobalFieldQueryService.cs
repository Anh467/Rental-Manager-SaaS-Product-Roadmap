using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.GlobalFields.Contracts;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Queries;

public sealed class GlobalFieldQueryService(
    IFieldRepository fields,
    IGlobalFieldOptionRepository options) : IGlobalFieldQueryService
{
    public async Task<PagedResult<FieldDto>> GetFieldsAsync(
        GetGlobalFieldsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = await fields.GetPagedAsync(
            request.IsActive,
            request.FieldTypeId,
            new PagedRequest(
                request.PageNumber ?? request.Page,
                request.PageSize,
                request.Search,
                request.SortBy,
                request.SortDirection),
            cancellationToken);

        var stored = await options.GetByFieldIdsAsync(
            page.Items.Select(x => x.Id).ToArray(),
            cancellationToken);

        return page.Map(field =>
            GlobalFieldDtoMapper.ToDto(
                field,
                stored.Where(x => x.FieldId == field.Id).ToArray()));
    }

    public async Task<FieldDto> GetFieldAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Field field = await fields.GetAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);

        return GlobalFieldDtoMapper.ToDto(
            field,
            await options.GetByFieldIdAsync(id, cancellationToken));
    }
}
