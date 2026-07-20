using RentalManager.Modules.TenantManagement.Core.Abstractions.Messaging;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Services;

namespace RentalManager.Modules.TenantManagement.Application.Tenants.SuspendTenant;

public sealed record SuspendTenantCommand(Guid TenantId) : ICommand;

public sealed class SuspendTenantCommandHandler
    : ICommandHandler<SuspendTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SuspendTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(
        SuspendTenantCommand command,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            command.TenantId,
            cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Tenant '{command.TenantId}' was not found.");

        tenant.Suspend(_clock.UtcNow);

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
