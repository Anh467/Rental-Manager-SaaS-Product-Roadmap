using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Queries;

public sealed record GetGlobalFieldQuery(Guid Id) : IQuery<FieldDto>;
