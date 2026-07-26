using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Queries;

public sealed class GetGlobalFieldQueryHandler(
    IFieldRepository fields,
    IGlobalFieldOptionRepository options)
    : IQueryHandler<GetGlobalFieldQuery, FieldDto>
{
    public async Task<FieldDto> HandleAsync(
        GetGlobalFieldQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        Field field = await fields.GetAsync(query.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);

        return GlobalFieldDtoMapper.ToDto(
            field,
            await options.GetByFieldIdAsync(query.Id, cancellationToken));
    }
}
