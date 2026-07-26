using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.GlobalFields.Contracts;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;

public sealed record UpdateGlobalFieldStatusCommand(
    Guid Id,
    UpdateGlobalFieldStatusRequest Request) : ICommand<FieldDto>;
