using RentalManager.Modules.TenantManagement.Domain.Common;

namespace RentalManager.Modules.TenantManagement.Domain.Tenants.Events;

public sealed record TenantStatusChangedDomainEvent(
    Guid EventId,
    Guid TenantId,
    TenantStatus PreviousStatus,
    TenantStatus CurrentStatus,
    DateTimeOffset OccurredAt) : IDomainEvent;
