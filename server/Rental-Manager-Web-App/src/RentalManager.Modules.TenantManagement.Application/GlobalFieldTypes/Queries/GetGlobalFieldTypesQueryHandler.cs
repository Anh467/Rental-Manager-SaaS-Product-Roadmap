using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFieldTypes.Queries;

public sealed class GetGlobalFieldTypesQueryHandler(IFieldTypeRepository fieldTypes)
    : IQueryHandler<GetGlobalFieldTypesQuery, IReadOnlyList<FieldTypeDto>>
{
    public async Task<IReadOnlyList<FieldTypeDto>> HandleAsync(
        GetGlobalFieldTypesQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Domain.Entities.Dbo.FieldType> items =
            await fieldTypes.GetAllAsync(cancellationToken);

        // Full catalogue from [dbo].[FieldType]. No DisplayOrder column — order by Id.
        return items
            .OrderBy(item => item.Id)
            .Select(item => new FieldTypeDto
            {
                Id = item.Id,
                Name = item.Name,
                Key = item.Key,
                Description = item.Description
            })
            .ToArray();
    }
}
