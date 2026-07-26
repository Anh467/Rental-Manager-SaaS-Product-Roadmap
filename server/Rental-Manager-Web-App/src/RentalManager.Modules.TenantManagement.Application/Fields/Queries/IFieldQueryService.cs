using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Queries;

public interface IFieldQueryService
{
    Task<PagedResult<FieldDto>> GetFieldsAsync(
        GetFieldsRequest request,
        CancellationToken cancellationToken = default);

    Task<FieldDto> GetFieldAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
