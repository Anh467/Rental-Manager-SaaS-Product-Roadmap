using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFieldTypes.Queries;

public interface IGlobalFieldTypeQueryService
{
    Task<IReadOnlyList<FieldTypeDto>> GetFieldTypesAsync(
        CancellationToken cancellationToken = default);
}
