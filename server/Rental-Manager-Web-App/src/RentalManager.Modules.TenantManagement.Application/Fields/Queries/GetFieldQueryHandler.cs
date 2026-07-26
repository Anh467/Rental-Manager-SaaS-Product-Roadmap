using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Queries;

public sealed class GetFieldQueryHandler : IQueryHandler<GetFieldQuery, FieldDto>
{
    private readonly IOrgFieldRepository _fields;
    private readonly IFieldOptionRepository _fieldOptions;

    public GetFieldQueryHandler(
        IOrgFieldRepository fields,
        IFieldOptionRepository fieldOptions)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(fieldOptions);

        _fields = fields;
        _fieldOptions = fieldOptions;
    }

    public async Task<FieldDto> HandleAsync(
        GetFieldQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        Field field = await _fields.GetAsync(query.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);

        IReadOnlyList<FieldOption> options = await _fieldOptions.GetByFieldIdAsync(
            field.Id,
            cancellationToken);

        return FieldDtoMapper.ToDto(field, options);
    }
}
