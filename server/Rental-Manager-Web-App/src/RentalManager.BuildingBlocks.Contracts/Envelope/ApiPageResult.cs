namespace RentalManager.BuildingBlocks.Contracts;

/// <summary>
/// Wire shape of a page, matching the client's <c>PageResult&lt;T&gt;</c>.
/// </summary>
public sealed record ApiPageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
