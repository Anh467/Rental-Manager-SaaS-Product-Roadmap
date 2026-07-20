namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.SqlServer.Options;

public sealed class TenantManagementDatabaseOptions
{
    public const string SectionName = "TenantManagement:Database";

    public required string ProviderInvariantName { get; init; }

    public required string WriteConnectionString { get; init; }

    public required string ReadConnectionString { get; init; }
}
