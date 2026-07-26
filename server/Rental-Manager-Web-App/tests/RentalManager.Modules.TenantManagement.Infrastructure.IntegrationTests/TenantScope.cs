using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.BuildingBlocks.Tenancy.Services;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// An organization context with nothing bound, used to prove that access to
/// <c>[org]</c> data fails closed rather than falling back to some default.
/// </summary>
internal sealed class UnboundOrganizationContext : IOrganizationContext
{
    public Guid? OrganizationId => null;

    public Guid? UserId => null;

    public string? CorrelationId => null;

    public bool HasOrganization => false;
}

/// <summary>
/// A composition root per tenant, wired exactly like the API but with the
/// organization supplied explicitly instead of taken from a request. One
/// instance owns one service provider; each unit of work takes its own scope and
/// therefore its own connection, transaction and session context.
/// </summary>
internal sealed class TenantScope : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;

    private TenantScope(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static TenantScope For(
        SqlServerFixture fixture,
        Guid organizationId,
        Guid? userId = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        return Create(
            fixture,
            new ExplicitOrganizationContext(
                organizationId,
                userId,
                correlationId: "integration-test"));
    }

    public static TenantScope Unbound(SqlServerFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        return Create(fixture, new UnboundOrganizationContext());
    }

    /// <summary>
    /// Starts one unit of work. Everything resolved from the returned scope
    /// shares a single connection and session context.
    /// </summary>
    public AsyncServiceScope BeginUnitOfWork()
    {
        return _serviceProvider.CreateAsyncScope();
    }

    public ValueTask DisposeAsync()
    {
        return _serviceProvider.DisposeAsync();
    }

    private static TenantScope Create(
        SqlServerFixture fixture,
        IOrganizationContext organizationContext)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RentalManager"] = fixture.ConnectionString
                })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(organizationContext);
        services.AddTenantManagementInfrastructure(configuration);

        return new TenantScope(
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true
                }));
    }
}
