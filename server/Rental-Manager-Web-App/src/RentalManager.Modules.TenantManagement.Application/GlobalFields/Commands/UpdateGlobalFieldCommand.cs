using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;

public sealed record UpdateGlobalFieldCommand(Guid Id, UpdateFieldRequest Request)
    : ICommand<FieldDto>;
