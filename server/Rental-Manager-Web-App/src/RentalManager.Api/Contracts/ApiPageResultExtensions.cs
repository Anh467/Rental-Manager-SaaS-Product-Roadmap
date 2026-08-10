using RentalManager.BuildingBlocks.Contracts;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

namespace RentalManager.Api.Contracts;

/// <summary>
/// Maps the application layer's <see cref="PagedResult{T}"/> onto the wire
/// shape <see cref="ApiPageResult{T}"/>. Kept in the Api project (rather than
/// BuildingBlocks.Contracts) because the envelope contracts must not depend
/// on any module's application layer.
/// </summary>
public static class ApiPageResultExtensions
{
    public static ApiPageResult<T> ToApiPageResult<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ApiPageResult<T>(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalItems,
            result.TotalPages);
    }
}
