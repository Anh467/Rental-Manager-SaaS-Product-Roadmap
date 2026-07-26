using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Queries;

public sealed class GetGlobalFieldsQueryHandler(
    IFieldRepository fields,
    IGlobalFieldOptionRepository options)
    : IQueryHandler<GetGlobalFieldsQuery, PagedResult<FieldDto>>
{
    public async Task<PagedResult<FieldDto>> HandleAsync(
        GetGlobalFieldsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var request = query.Request;
        ArgumentNullException.ThrowIfNull(request);

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
}
