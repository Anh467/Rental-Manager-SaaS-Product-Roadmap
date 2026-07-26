using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.GlobalFields.Contracts;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Queries;

public interface IGlobalFieldQueryService
{
    Task<PagedResult<FieldDto>> GetFieldsAsync(
        GetGlobalFieldsRequest request,
        CancellationToken cancellationToken = default);

    Task<FieldDto> GetFieldAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
