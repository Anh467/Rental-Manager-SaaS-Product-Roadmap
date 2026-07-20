using RentalManager.Modules.TenantManagement.Core.Abstractions.Services;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
