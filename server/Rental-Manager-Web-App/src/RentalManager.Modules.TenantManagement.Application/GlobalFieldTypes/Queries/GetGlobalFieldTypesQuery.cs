using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFieldTypes.Queries;

public sealed record GetGlobalFieldTypesQuery() : IQuery<IReadOnlyList<FieldTypeDto>>;
