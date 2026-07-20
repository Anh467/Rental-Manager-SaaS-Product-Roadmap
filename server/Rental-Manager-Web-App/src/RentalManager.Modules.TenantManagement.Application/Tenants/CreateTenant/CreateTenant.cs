using RentalManager.Modules.TenantManagement.Core.Abstractions.Messaging;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Core.Abstractions.Services;
using RentalManager.Modules.TenantManagement.Domain.Tenants;

namespace RentalManager.Modules.TenantManagement.Application.Tenants.CreateTenant;

public sealed record CreateTenantCommand(
    string Code,
    string Name) : ICommand<CreateTenantResult>;

public sealed record CreateTenantResult(Guid TenantId);

public sealed class CreateTenantCommandHandler
    : ICommandHandler<CreateTenantCommand, CreateTenantResult>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<CreateTenantResult> HandleAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken)
    {
        bool codeExists = await _tenantRepository.CodeExistsAsync(
            command.Code,
            cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException(
                $"Tenant code '{command.Code}' already exists.");
        }

        Tenant tenant = Tenant.Create(
            command.Code,
            command.Name,
            _clock.UtcNow);

        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new CreateTenantResult(tenant.Id);
    }
}
