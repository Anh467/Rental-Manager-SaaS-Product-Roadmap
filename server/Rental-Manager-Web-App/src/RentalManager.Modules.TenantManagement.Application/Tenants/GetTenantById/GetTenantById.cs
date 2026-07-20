using RentalManager.Modules.TenantManagement.Core.Abstractions.Messaging;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;

namespace RentalManager.Modules.TenantManagement.Application.Tenants.GetTenantById;

public sealed record GetTenantByIdQuery(
    Guid TenantId) : IQuery<TenantDetailsResponse?>;

public sealed record TenantDetailsResponse(
    Guid TenantId,
    string Code,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);

public sealed class GetTenantByIdQueryHandler
    : IQueryHandler<GetTenantByIdQuery, TenantDetailsResponse?>
{
    private readonly ITenantReadStore _tenantReadStore;

    public GetTenantByIdQueryHandler(ITenantReadStore tenantReadStore)
    {
        _tenantReadStore = tenantReadStore;
    }

    public async Task<TenantDetailsResponse?> HandleAsync(
        GetTenantByIdQuery query,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantReadStore.GetByIdAsync(
            query.TenantId,
            cancellationToken);

        return tenant is null
            ? null
            : new TenantDetailsResponse(
                tenant.TenantId,
                tenant.Code,
                tenant.Name,
                tenant.Status,
                tenant.CreatedAt);
    }
}
