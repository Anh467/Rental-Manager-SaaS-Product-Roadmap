namespace RentalManager.Modules.TenantManagement.Core.Contracts.Tenants;

public sealed record TenantDetails(
    Guid TenantId,
    string Code,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record TenantListItem(
    Guid TenantId,
    string Code,
    string Name,
    string Status);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    long TotalCount);
