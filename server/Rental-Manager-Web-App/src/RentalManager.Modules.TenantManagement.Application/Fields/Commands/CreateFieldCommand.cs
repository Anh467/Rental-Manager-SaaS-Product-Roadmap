using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Commands;

public sealed record CreateFieldCommand(CreateFieldRequest Request) : ICommand<FieldDto>;
