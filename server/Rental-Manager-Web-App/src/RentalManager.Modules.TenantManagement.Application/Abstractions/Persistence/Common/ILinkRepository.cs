using RentalManager.Modules.TenantManagement.Domain.Entities.Common;

namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common
{
    public interface ILinkRepository<in TEntity> where TEntity : ILink
    {
        Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    }
}
