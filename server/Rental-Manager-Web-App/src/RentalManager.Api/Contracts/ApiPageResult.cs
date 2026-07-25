using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

namespace RentalManager.Api.Contracts;

/// <summary>
/// Wire shape of a page, matching the client's <c>PageResult&lt;T&gt;</c>.
/// </summary>
public sealed record ApiPageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static ApiPageResult<T> From(PagedResult<T> result)
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
