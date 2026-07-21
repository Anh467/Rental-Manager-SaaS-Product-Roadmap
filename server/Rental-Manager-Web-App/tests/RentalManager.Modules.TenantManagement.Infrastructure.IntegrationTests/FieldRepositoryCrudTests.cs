using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Infrastructure;
using Xunit;
using FieldEntity = RentalManager.Modules.TenantManagement.Domain.Entities.Dbo.Field;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class FieldRepositoryCrudTests
{
    private readonly SqlServerFixture _fixture;

    public FieldRepositoryCrudTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FieldRepository_CreateReadUpdateListDelete_Works()
    {
        await _fixture.ResetFieldsAsync();
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await using AsyncServiceScope scope =
            serviceProvider.CreateAsyncScope();

        IFieldRepository repository =
            scope.ServiceProvider.GetRequiredService<IFieldRepository>();

        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        var field = new FieldEntity
        {
            Id = Guid.NewGuid(),
            Key = "RENTER_PHONE",
            Name = "Renter phone",
            Description = "Phone number of the renter.",
            FieldTypeId = 1,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        await repository.SaveAsync(field);

        FieldEntity created = await repository.GetAsync(field.Id);

        Assert.Equal(field.Id, created.Id);
        Assert.Equal("RENTER_PHONE", created.Key);
        Assert.Equal("Renter phone", created.Name);
        Assert.Equal(1, created.FieldTypeId);
        Assert.Null(created.DeletedAt);

        field.Name = "Tenant phone";
        field.Description = "Primary phone number of the tenant.";
        field.UpdatedAt = createdAt.AddMinutes(1);

        await repository.SaveAsync(field);

        FieldEntity updated = await repository.GetAsync(field.Id);

        Assert.Equal("Tenant phone", updated.Name);
        Assert.Equal(
            "Primary phone number of the tenant.",
            updated.Description);
        Assert.Equal(field.UpdatedAt, updated.UpdatedAt);

        IReadOnlyCollection<FieldEntity> fields =
            await repository.GetAllAsync();

        FieldEntity listed = Assert.Single(fields);
        Assert.Equal(field.Id, listed.Id);

        await repository.DeleteAsync(field);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.GetAsync(field.Id));

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task FieldRepository_SoftDelete_HidesDeletedField()
    {
        await _fixture.ResetFieldsAsync();
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await using AsyncServiceScope scope =
            serviceProvider.CreateAsyncScope();

        IFieldRepository repository =
            scope.ServiceProvider.GetRequiredService<IFieldRepository>();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var field = new FieldEntity
        {
            Id = Guid.NewGuid(),
            Key = "MOVE_IN_DATE",
            Name = "Move-in date",
            Description = "The renter's move-in date.",
            FieldTypeId = 3,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.SaveAsync(field);
        await repository.SoftDeleteAsync(field.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.GetAsync(field.Id));

        Assert.Empty(await repository.GetAllAsync());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.SoftDeleteAsync(field.Id));
    }

    private ServiceProvider CreateServiceProvider()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RentalManager"] =
                        _fixture.ConnectionString
                })
            .Build();

        var services = new ServiceCollection();
        services.AddTenantManagementInfrastructure(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });
    }
}
