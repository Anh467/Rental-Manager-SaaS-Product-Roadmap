using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;

public sealed record CreateGlobalFieldCommand(CreateFieldRequest Request) : ICommand<FieldDto>;
