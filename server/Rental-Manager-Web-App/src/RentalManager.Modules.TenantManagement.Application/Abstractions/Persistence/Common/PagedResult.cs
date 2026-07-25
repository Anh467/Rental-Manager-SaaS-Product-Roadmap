namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;

/// <summary>
/// Page envelope shared by every list endpoint. Shape matches the client
/// <c>PageResult&lt;T&gt;</c> contract.
/// </summary>
public sealed class PagedResult<T>
{
    public PagedResult(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalItems { get; }

    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public PagedResult<TTarget> Map<TTarget>(Func<T, TTarget> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return new PagedResult<TTarget>(
            Items.Select(selector).ToArray(),
            Page,
            PageSize,
            TotalItems);
    }
}
