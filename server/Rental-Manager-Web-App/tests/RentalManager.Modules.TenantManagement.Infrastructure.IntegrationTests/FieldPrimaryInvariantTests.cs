using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using Xunit;
using FieldEntity = RentalManager.Modules.TenantManagement.Domain.Entities.Org.Field;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// The primary display field invariant, verified against the database that
/// actually enforces it through <c>UX_Field_ActivePrimary</c>.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class FieldPrimaryInvariantTests
{
    private readonly SqlServerFixture _fixture;

    public FieldPrimaryInvariantTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Primary_moves_atomically_and_the_scope_keeps_exactly_one()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto first = await tenant.CreateTextFieldAsync("room_number");
        FieldDto second = await tenant.CreateTextFieldAsync("floor");

        FieldDto promoted = await tenant.UpdateAsync(second.Id, new UpdateFieldRequest
        {
            Name = second.Name,
            IsPrimaryDisplayField = true,
            RowVersion = second.RowVersion
        });

        Assert.True(promoted.IsPrimaryDisplayField);
        Assert.False((await tenant.GetAsync(first.Id)).IsPrimaryDisplayField);
        Assert.Equal(1, await CountActivePrimariesAsync());
    }

    [Fact]
    public async Task Demoting_the_last_primary_is_refused_without_a_replacement()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto primary = await tenant.CreateTextFieldAsync("room_number");
        await tenant.CreateTextFieldAsync("floor");

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => tenant.UpdateAsync(primary.Id, new UpdateFieldRequest
                {
                    Name = primary.Name,
                    IsPrimaryDisplayField = false,
                    RowVersion = primary.RowVersion
                }));

        Assert.Equal(
            MessageCode.Error.LastActivePrimaryFieldRemoval,
            exception.MessageKey);

        Assert.True((await tenant.GetAsync(primary.Id)).IsPrimaryDisplayField);
        Assert.Equal(1, await CountActivePrimariesAsync());
    }

    [Fact]
    public async Task Deactivating_the_last_primary_is_refused_without_a_replacement()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto primary = await tenant.CreateTextFieldAsync("room_number");
        await tenant.CreateTextFieldAsync("floor");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => tenant.UpdateAsync(primary.Id, new UpdateFieldRequest
            {
                Name = primary.Name,
                IsActive = false,
                RowVersion = primary.RowVersion
            }));

        Assert.True((await tenant.GetAsync(primary.Id)).IsActive);
    }

    [Fact]
    public async Task Deleting_the_last_primary_is_refused_without_a_replacement()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto primary = await tenant.CreateTextFieldAsync("room_number");
        await tenant.CreateTextFieldAsync("floor");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => tenant.DeleteAsync(primary.Id, new DeleteFieldRequest
            {
                RowVersion = primary.RowVersion
            }));

        Assert.NotNull(await tenant.GetAsync(primary.Id));
        Assert.Equal(1, await CountActivePrimariesAsync());
    }

    [Fact]
    public async Task Deleting_the_primary_with_a_replacement_hands_the_flag_over()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto primary = await tenant.CreateTextFieldAsync("room_number");
        FieldDto replacement = await tenant.CreateTextFieldAsync("floor");

        await tenant.DeleteAsync(primary.Id, new DeleteFieldRequest
        {
            RowVersion = primary.RowVersion,
            ReplacementPrimaryFieldId = replacement.Id
        });

        Assert.True((await tenant.GetAsync(replacement.Id)).IsPrimaryDisplayField);
        Assert.Equal(1, await CountActivePrimariesAsync());
    }

    [Fact]
    public async Task Replacement_from_another_scope_is_refused()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto primary = await tenant.CreateTextFieldAsync("room_number");
        await tenant.CreateTextFieldAsync("floor");

        FieldDto otherScope = await tenant.CreateTextFieldAsync(
            "nickname",
            TestData.TargetEntityType.Person);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => tenant.DeleteAsync(primary.Id, new DeleteFieldRequest
            {
                RowVersion = primary.RowVersion,
                ReplacementPrimaryFieldId = otherScope.Id
            }));
    }

    /// <summary>
    /// The database refuses a second active primary even when a caller bypasses
    /// the service and writes through the repository, which is what makes the
    /// invariant real rather than advisory.
    /// </summary>
    [Fact]
    public async Task Database_refuses_a_second_active_primary_in_the_same_scope()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        await tenant.CreateTextFieldAsync("room_number");

        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        var session = scope.ServiceProvider.GetRequiredService<ISqlSession>();
        var fields = scope.ServiceProvider.GetRequiredService<IOrgFieldRepository>();

        await using ISqlTransactionScope transaction =
            await session.BeginTransactionAsync();

        var second = new FieldEntity
        {
            Id = Guid.CreateVersion7(),
            TargetEntityType = TestData.TargetEntityType.Room,
            Key = "floor",
            NormalizedKey = "FLOOR",
            Name = "Floor",
            FieldTypeId = (int)EFieldType.Text,
            IsPrimaryDisplayField = true,
            IsActive = true
        };

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => fields.InsertAsync(second));

        Assert.Equal(
            MessageCode.Error.MultipleActivePrimaryFields,
            exception.MessageKey);
    }

    private Task<int> CountActivePrimariesAsync()
    {
        return _fixture.CountRowsIgnoringSecurityAsync(
            "[org].[Field]",
            $"[OrganizationId] = '{TestData.OrganizationA.Id}' " +
            "AND [TargetEntityType] = N'Room' " +
            "AND [IsPrimaryDisplayField] = 1 " +
            "AND [IsActive] = 1 " +
            "AND [DeletedAt] IS NULL");
    }

    private TenantScope OrganizationA()
    {
        return TenantScope.For(
            _fixture,
            TestData.OrganizationA.Id,
            TestData.Users.AdministratorAId);
    }
}
