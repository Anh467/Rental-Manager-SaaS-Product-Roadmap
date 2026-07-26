using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields;

public sealed class GlobalFieldTypeService(IFieldTypeRepository fieldTypes) : IGlobalFieldTypeService
{
    public async Task<IReadOnlyList<FieldTypeDto>> GetFieldTypesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Domain.Entities.Dbo.FieldType> items =
            await fieldTypes.GetAllAsync(cancellationToken);

        // Read from [dbo].[FieldType], then apply the same create/list support
        // rules as FieldInvariants. Table has no DisplayOrder — order by Id.
        return items
            .Where(item => FieldInvariants.IsSupportedFieldType(item.Id))
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
