using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using Xunit;
using FieldEntity = RentalManager.Modules.TenantManagement.Domain.Entities.Dbo.Field;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Smoke test for the global path. <c>[dbo].[Field]</c> is the field template
/// catalogue: it has no <c>OrganizationId</c>, so it proves the common repository
/// still behaves for an entity that is not organization owned.
/// </summary>
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
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = GlobalScope();
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        IFieldRepository repository =
            scope.ServiceProvider.GetRequiredService<IFieldRepository>();

        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        var field = new FieldEntity
        {
            Id = Guid.NewGuid(),
            Key = "renter_phone",
            Name = "Renter phone",
            Description = "Phone number of the renter.",
            FieldTypeId = 1,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        await repository.InsertAsync(field);

        FieldEntity created = Assert.IsType<FieldEntity>(
            await repository.GetAsync(field.Id));

        Assert.Equal(field.Id, created.Id);
        Assert.Equal("renter_phone", created.Key);
        Assert.Equal("Renter phone", created.Name);
        Assert.Equal(1, created.FieldTypeId);
        Assert.Null(created.DeletedAt);
        Assert.NotEmpty(created.RowVersion);

        field.Name = "Tenant phone";
        field.Description = "Primary phone number of the tenant.";
        field.UpdatedAt = createdAt.AddMinutes(1);
        field.RowVersion = created.RowVersion;

        await repository.UpdateAsync(field, created.RowVersion);

        FieldEntity updated = Assert.IsType<FieldEntity>(
            await repository.GetAsync(field.Id));

        Assert.Equal("Tenant phone", updated.Name);
        Assert.Equal(
            "Primary phone number of the tenant.",
            updated.Description);
        Assert.NotEqual(created.RowVersion, updated.RowVersion);

        IReadOnlyCollection<FieldEntity> fields =
            await repository.GetAllAsync();

        FieldEntity listed = Assert.Single(fields);
        Assert.Equal(field.Id, listed.Id);

        await repository.DeleteAsync(field);

        Assert.Null(await repository.GetAsync(field.Id));
        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task FieldRepository_SoftDelete_HidesDeletedField()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = GlobalScope();
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        IFieldRepository repository =
            scope.ServiceProvider.GetRequiredService<IFieldRepository>();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var field = new FieldEntity
        {
            Id = Guid.NewGuid(),
            Key = "move_in_date",
            Name = "Move-in date",
            Description = "The renter's move-in date.",
            FieldTypeId = 3,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.InsertAsync(field);

        FieldEntity stored = Assert.IsType<FieldEntity>(
            await repository.GetAsync(field.Id));

        await repository.SoftDeleteAsync(field.Id, stored.RowVersion);

        Assert.Null(await repository.GetAsync(field.Id));
        Assert.Empty(await repository.GetAllAsync());

        await Assert.ThrowsAsync<Core.Exceptions.ResourceNotFoundException>(
            () => repository.SoftDeleteAsync(field.Id, stored.RowVersion));
    }

    private TenantScope GlobalScope()
    {
        return TenantScope.Unbound(_fixture);
    }
}
