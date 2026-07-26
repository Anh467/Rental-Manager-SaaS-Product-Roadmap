using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Queries;

public sealed class GetFieldsQueryHandler : IQueryHandler<GetFieldsQuery, PagedResult<FieldDto>>
{
    private readonly IOrgFieldRepository _fields;
    private readonly IFieldOptionRepository _fieldOptions;

    public GetFieldsQueryHandler(
        IOrgFieldRepository fields,
        IFieldOptionRepository fieldOptions)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(fieldOptions);

        _fields = fields;
        _fieldOptions = fieldOptions;
    }

    public async Task<PagedResult<FieldDto>> HandleAsync(
        GetFieldsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        GetFieldsRequest request = query.Request;
        ArgumentNullException.ThrowIfNull(request);

        var pagedRequest = new PagedRequest(
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.SortDirection);

        PagedResult<Field> page = await _fields.GetPagedAsync(
            request.IsActive,
            pagedRequest,
            cancellationToken);

        Guid[] fieldIds = page.Items.Select(field => field.Id).ToArray();
        IReadOnlyList<FieldOption> options = await _fieldOptions.GetByFieldIdsAsync(
            fieldIds,
            cancellationToken);

        Dictionary<Guid, List<FieldOption>> optionsByField = options
            .GroupBy(option => option.FieldId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return page.Map(field => FieldDtoMapper.ToDto(
            field,
            optionsByField.TryGetValue(field.Id, out List<FieldOption>? fieldOptions)
                ? fieldOptions
                : []));
    }
}
