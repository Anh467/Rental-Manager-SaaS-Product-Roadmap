namespace RentalManager.Modules.TenantManagement.Core.Abstractions.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
