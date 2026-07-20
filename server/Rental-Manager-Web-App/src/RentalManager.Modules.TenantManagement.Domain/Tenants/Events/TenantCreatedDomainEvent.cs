using RentalManager.Modules.TenantManagement.Domain.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Tenants.Events;

public sealed record TenantCreatedDomainEvent(
    Guid EventId,
    Guid TenantId,
    string Code,
    string Name,
    DateTimeOffset OccurredAt) : IDomainEvent;
