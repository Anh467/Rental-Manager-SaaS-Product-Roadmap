namespace RentalManager.Modules.TenantManagement.Domain.Tenants;

public sealed class Tenant
{
    private Tenant()
    {
    }

    public Guid Id { get; private set; }

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
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Status = TenantStatus.Active,
            CreatedAt = createdAt
        };
    }

    public void UpdateName(string name, DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        UpdatedAt = updatedAt;
    }

    public void Suspend(DateTimeOffset updatedAt)
    {
        Status = TenantStatus.Suspended;
        UpdatedAt = updatedAt;
    }

    public void Activate(DateTimeOffset updatedAt)
    {
        Status = TenantStatus.Active;
        UpdatedAt = updatedAt;
    }
}
