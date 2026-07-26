using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields;

public interface IGlobalFieldTypeService
{
    Task<IReadOnlyList<FieldTypeDto>> GetFieldTypesAsync(
        CancellationToken cancellationToken = default);
}
