using RentalManager.Modules.TenantManagement.Domain.Common;
using RentalManager.Modules.TenantManagement.Domain.Tenants.Events;

namespace RentalManager.Modules.TenantManagement.Domain.Tenants;

public sealed class Tenant : AggregateRoot<Guid>
{
    private Tenant()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public TenantStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Tenant Create(
        string code,
        string name,
        DateTimeOffset createdAt)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Code = TenantRules.NormalizeCode(code),
            Name = TenantRules.NormalizeName(name),
            Status = TenantStatus.Active,
            CreatedAt = createdAt
        };

        tenant.RaiseDomainEvent(
            new TenantCreatedDomainEvent(
                Guid.NewGuid(),
                tenant.Id,
                tenant.Code,
                tenant.Name,
                createdAt));

        return tenant;
    }

    public static Tenant Rehydrate(
        Guid id,
        string code,
        string name,
        TenantStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
    {
        return new Tenant
        {
            Id = id,
            Code = TenantRules.NormalizeCode(code),
            Name = TenantRules.NormalizeName(name),
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void UpdateName(string name, DateTimeOffset updatedAt)
    {
        Name = TenantRules.NormalizeName(name);
        UpdatedAt = updatedAt;
    }

    public void Suspend(DateTimeOffset updatedAt)
    {
        ChangeStatus(TenantStatus.Suspended, updatedAt);
    }

    public void Activate(DateTimeOffset updatedAt)
    {
        ChangeStatus(TenantStatus.Active, updatedAt);
    }

    private void ChangeStatus(
        TenantStatus newStatus,
        DateTimeOffset changedAt)
    {
        if (Status == newStatus)
        {
            return;
        }

        TenantStatus previousStatus = Status;
        Status = newStatus;
        UpdatedAt = changedAt;

        RaiseDomainEvent(
            new TenantStatusChangedDomainEvent(
                Guid.NewGuid(),
                Id,
                previousStatus,
                newStatus,
                changedAt));
    }
}
