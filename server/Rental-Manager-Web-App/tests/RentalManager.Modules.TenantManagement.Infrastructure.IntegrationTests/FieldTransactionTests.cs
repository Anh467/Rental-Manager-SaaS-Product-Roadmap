using Microsoft.Extensions.DependencyInjection;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Auditing;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using Xunit;
using FieldEntity = RentalManager.Modules.TenantManagement.Domain.Entities.Org.Field;
using FieldOptionEntity = RentalManager.Modules.TenantManagement.Domain.Entities.Org.FieldOption;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Everything one field use case touches has to live or die together: the field,
/// its options and its audit entry.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class FieldTransactionTests
{
    private readonly SqlServerFixture _fixture;

    public FieldTransactionTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Field_options_and_audit_entry_are_committed_together()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateMultiSelectFieldAsync(
            "amenities",
            [
                new FieldOptionInput { Key = "wifi", Name = "Wi-Fi" },
                new FieldOptionInput { Key = "parking", Name = "Parking", DisplayOrder = 1 }
            ]);

        Assert.Equal(2, field.Options.Count);

        Assert.Equal(1, await CountFieldsAsync());
        Assert.Equal(2, await CountOptionsAsync($"[FieldId] = '{field.Id}'"));
        Assert.Equal(1, await CountAuditEntriesAsync(field.Id, AuditAction.Created));

        FieldDto reloaded = await tenant.GetAsync(field.Id);
        Assert.Equal(["wifi", "parking"], reloaded.Options.Select(option => option.Key));
    }

    [Fact]
    public async Task Removing_an_option_retires_it_within_the_same_transaction()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateMultiSelectFieldAsync(
            "amenities",
            [
                new FieldOptionInput { Key = "wifi", Name = "Wi-Fi" },
                new FieldOptionInput { Key = "parking", Name = "Parking", DisplayOrder = 1 }
            ]);

        FieldDto updated = await tenant.UpdateAsync(field.Id, new UpdateFieldRequest
        {
            Name = field.Name,
            IsPrimaryDisplayField = true,
            RowVersion = field.RowVersion,
            Options = [new FieldOptionInput { Key = "wifi", Name = "Wi-Fi" }]
        });

        Assert.Equal("wifi", Assert.Single(updated.Options).Key);

        Assert.Equal(
            1,
            await CountOptionsAsync(
                $"[FieldId] = '{field.Id}' AND [DeletedAt] IS NOT NULL"));
    }

    [Fact]
    public async Task Deleting_a_field_retires_its_options_in_the_same_transaction()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateMultiSelectFieldAsync(
            "amenities",
            [new FieldOptionInput { Key = "wifi", Name = "Wi-Fi" }]);

        await tenant.DeleteAsync(
            field.Id,
            new DeleteFieldRequest { RowVersion = field.RowVersion });

        Assert.Equal(
            1,
            await CountOptionsAsync(
                $"[FieldId] = '{field.Id}' AND [DeletedAt] IS NOT NULL"));

        Assert.Equal(1, await CountAuditEntriesAsync(field.Id, AuditAction.Deleted));
    }

    /// <summary>
    /// A failure part way through a compound write must leave nothing behind,
    /// including the option rows that had already been inserted.
    /// </summary>
    [Fact]
    public async Task Field_options_are_rolled_back_when_a_later_write_fails()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        await RunAndDiscardAsync(tenant, async (fields, options, _) =>
        {
            FieldEntity field = NewField("amenities", EFieldType.MultiSelect);

            await fields.InsertAsync(field);
            await options.InsertAsync(NewOption(field.Id, "wifi"));

            // Same key again: the unique constraint rejects it, which aborts the
            // use case after the option has already been written.
            await Assert.ThrowsAsync<DuplicateResourceException>(
                () => fields.InsertAsync(NewField("amenities", EFieldType.MultiSelect)));
        });

        Assert.Equal(0, await CountFieldsAsync());
        Assert.Equal(0, await CountOptionsAsync());
    }

    [Fact]
    public async Task Audit_entry_is_rolled_back_with_the_field_transaction()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        Guid fieldId = Guid.Empty;

        await RunAndDiscardAsync(tenant, async (fields, _, auditLog) =>
        {
            FieldEntity field = NewField("room_number", EFieldType.Text);
            fieldId = field.Id;

            await fields.InsertAsync(field);

            await auditLog.WriteAsync(
                new AuditLogEntry(
                    "Field",
                    field.Id,
                    AuditAction.Created,
                    "Name=Room number"));
        });

        Assert.Equal(0, await CountFieldsAsync());
        Assert.Equal(0, await CountAuditEntriesAsync(fieldId, AuditAction.Created));
    }

    /// <summary>
    /// Runs a unit of work and disposes the transaction without committing, which
    /// is exactly what happens when an exception escapes the service.
    /// </summary>
    private async Task RunAndDiscardAsync(
        TenantScope tenant,
        Func<IOrgFieldRepository, IFieldOptionRepository, IAuditLogWriter, Task> body)
    {
        await using AsyncServiceScope scope = tenant.BeginUnitOfWork();

        var session = scope.ServiceProvider.GetRequiredService<ISqlSession>();

        await using ISqlTransactionScope transaction =
            await session.BeginTransactionAsync();

        await body(
            scope.ServiceProvider.GetRequiredService<IOrgFieldRepository>(),
            scope.ServiceProvider.GetRequiredService<IFieldOptionRepository>(),
            scope.ServiceProvider.GetRequiredService<IAuditLogWriter>());
    }

    private static FieldEntity NewField(string key, EFieldType fieldType)
    {
        return new FieldEntity
        {
            Id = Guid.CreateVersion7(),
            TargetEntityType = TestData.TargetEntityType.Room,
            Key = key,
            NormalizedKey = key.ToUpperInvariant(),
            Name = key,
            FieldTypeId = (int)fieldType
        };
    }

    private static FieldOptionEntity NewOption(Guid fieldId, string key)
    {
        return new FieldOptionEntity
        {
            Id = Guid.CreateVersion7(),
            FieldId = fieldId,
            Key = key,
            NormalizedKey = key.ToUpperInvariant(),
            Name = key
        };
    }

    private Task<int> CountFieldsAsync()
    {
        return _fixture.CountRowsIgnoringSecurityAsync("[org].[Field]");
    }

    private Task<int> CountOptionsAsync(string? predicate = null)
    {
        return _fixture.CountRowsIgnoringSecurityAsync(
            "[org].[FieldOption]",
            predicate);
    }

    private Task<int> CountAuditEntriesAsync(Guid entityId, string action)
    {
        return _fixture.CountRowsIgnoringSecurityAsync(
            "[org].[AuditLog]",
            $"[EntityId] = '{entityId}' AND [Action] = N'{action}'");
    }

    private TenantScope OrganizationA()
    {
        return TenantScope.For(
            _fixture,
            TestData.OrganizationA.Id,
            TestData.Users.AdministratorAId);
    }
}
