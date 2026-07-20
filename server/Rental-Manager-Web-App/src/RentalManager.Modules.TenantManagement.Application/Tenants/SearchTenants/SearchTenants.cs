using RentalManager.Modules.TenantManagement.Core.Abstractions.Messaging;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Core.Contracts.Tenants;

namespace RentalManager.Modules.TenantManagement.Application.Tenants.SearchTenants;

public sealed record SearchTenantsQuery(
    string? Keyword,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<TenantListItem>>;

public sealed class SearchTenantsQueryHandler
    : IQueryHandler<SearchTenantsQuery, PagedResult<TenantListItem>>
{
    private const int MaximumPageSize = 100;
    private readonly ITenantReadStore _tenantReadStore;

    public SearchTenantsQueryHandler(ITenantReadStore tenantReadStore)
    {
        _tenantReadStore = tenantReadStore;
    }

    public Task<PagedResult<TenantListItem>> HandleAsync(
        SearchTenantsQuery query,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        string? keyword = string.IsNullOrWhiteSpace(query.Keyword)
            ? null
            : query.Keyword.Trim();

        return _tenantReadStore.SearchAsync(
            keyword,
            page,
            pageSize,
            cancellationToken);
    }
}
