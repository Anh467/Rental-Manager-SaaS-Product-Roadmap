using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields;

public interface IGlobalFieldTypeService
{
    Task<IReadOnlyList<FieldTypeDto>> GetFieldTypesAsync(
        CancellationToken cancellationToken = default);
}
