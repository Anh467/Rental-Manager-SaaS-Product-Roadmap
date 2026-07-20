using RentalManager.Modules.TenantManagement.Core.Abstractions.Messaging;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Services;

namespace RentalManager.Modules.TenantManagement.Application.Tenants.UpdateTenant;

public sealed record UpdateTenantCommand(
    Guid TenantId,
    string Name) : ICommand;

public sealed class UpdateTenantCommandHandler
    : ICommandHandler<UpdateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(
        UpdateTenantCommand command,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            command.TenantId,
            cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Tenant '{command.TenantId}' was not found.");

        tenant.UpdateName(command.Name, _clock.UtcNow);

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
