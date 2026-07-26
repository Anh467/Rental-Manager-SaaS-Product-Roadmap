using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Queries;

public sealed record GetFieldsQuery(GetFieldsRequest Request)
    : IQuery<PagedResult<FieldDto>>;
