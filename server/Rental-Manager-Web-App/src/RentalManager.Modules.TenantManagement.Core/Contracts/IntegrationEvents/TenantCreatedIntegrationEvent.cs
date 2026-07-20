namespace RentalManager.Modules.TenantManagement.Core.Contracts.IntegrationEvents;

public sealed record TenantCreatedIntegrationEvent(
    Guid EventId,
    Guid TenantId,
    string Code,
    string Name,
    DateTimeOffset OccurredAt);
