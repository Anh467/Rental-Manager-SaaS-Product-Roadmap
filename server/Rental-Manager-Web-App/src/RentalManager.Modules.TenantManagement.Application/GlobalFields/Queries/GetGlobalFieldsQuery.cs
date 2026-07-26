using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.GlobalFields.Contracts;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Queries;

public sealed record GetGlobalFieldsQuery(GetGlobalFieldsRequest Request)
    : IQuery<PagedResult<FieldDto>>;
