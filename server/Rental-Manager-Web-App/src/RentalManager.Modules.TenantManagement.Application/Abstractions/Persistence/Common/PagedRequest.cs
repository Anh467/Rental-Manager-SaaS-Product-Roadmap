namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

/// <summary>
/// Normalized paging, search and sorting input shared by every list endpoint.
/// Values are clamped on construction so no caller can request an unbounded
/// page.
/// </summary>
public sealed class PagedRequest
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;
    public const string DescendingDirection = "desc";

    public PagedRequest(
        int? page = null,
        int? pageSize = null,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null)
    {
        Page = page is null || page < 1 ? 1 : page.Value;

        PageSize = pageSize switch
        {
            null => DefaultPageSize,
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value
        };

        Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        SortBy = string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.Trim();
        SortDirection = string.Equals(
            sortDirection,
            DescendingDirection,
            StringComparison.OrdinalIgnoreCase)
            ? DescendingDirection
            : "asc";
    }

    public int Page { get; }

    public int PageSize { get; }

    public string? Search { get; }

    public string? SortBy { get; }

    public string SortDirection { get; }

    public bool IsDescending => string.Equals(
        SortDirection,
        DescendingDirection,
        StringComparison.OrdinalIgnoreCase);

    public int Offset => (Page - 1) * PageSize;
}
