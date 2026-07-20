using System.Threading;

namespace RentalManager.BuildingBlocks.Tenancy;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> CurrentTenant = new();

    public TenantContext? Current
    {
        get => CurrentTenant.Value;
        set => CurrentTenant.Value = value;
    }
}
