using System.Data.Common;
using RentalManager.Modules.TenantManagement.Core.Contracts.Tenants;
using RentalManager.Modules.TenantManagement.Domain.Tenants;

namespace RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;

public interface ITenantRepository
{
    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken);

    Task<Tenant?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Tenant tenant,
        CancellationToken cancellationToken);
}

public interface ITenantReadStore
{
    Task<TenantDetails?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<PagedResult<TenantListItem>> SearchAsync(
        string? keyword,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);
}

public interface IWriteDbSession : IAsyncDisposable
{
    DbConnection Connection { get; }

    DbTransaction? Transaction { get; }

    Task OpenAsync(CancellationToken cancellationToken);
}

public interface IReadDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default);
}
