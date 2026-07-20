using RentalManager.Modules.TenantManagement.Core.Abstractions.Messaging;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Services;

namespace RentalManager.Modules.TenantManagement.Application.Tenants.ActivateTenant;

public sealed record ActivateTenantCommand(Guid TenantId) : ICommand;

public sealed class ActivateTenantCommandHandler
    : ICommandHandler<ActivateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ActivateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(
        ActivateTenantCommand command,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            command.TenantId,
            cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Tenant '{command.TenantId}' was not found.");

        tenant.Activate(_clock.UtcNow);

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
