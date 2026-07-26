using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Queries;

public sealed record GetFieldQuery(Guid Id) : IQuery<FieldDto>;
