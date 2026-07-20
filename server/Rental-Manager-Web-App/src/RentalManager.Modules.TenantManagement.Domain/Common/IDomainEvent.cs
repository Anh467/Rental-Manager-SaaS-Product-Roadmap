namespace RentalManager.Modules.TenantManagement.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
