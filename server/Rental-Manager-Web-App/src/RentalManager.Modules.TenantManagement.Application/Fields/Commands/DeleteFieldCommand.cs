using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Commands;

public sealed record DeleteFieldCommand(Guid Id, DeleteFieldRequest Request) : ICommand;
