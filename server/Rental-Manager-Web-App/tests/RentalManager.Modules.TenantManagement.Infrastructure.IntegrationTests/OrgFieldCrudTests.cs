using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Field create, read, update and soft delete against a real SQL Server, using
/// the schema deployed from the DACPAC.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class OrgFieldCrudTests
{
    private readonly SqlServerFixture _fixture;

    public OrgFieldCrudTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_persists_the_field_for_the_calling_organization()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateAsync(new CreateFieldRequest
        {
            TargetEntityType = TestData.TargetEntityType.Room,
            Key = "  room_number ",
            Name = " Room number ",
            Description = "Number shown on the door.",
            FieldTypeId = (int)EFieldType.Text,
            IsRequired = true
        });

        Assert.Equal("room_number", field.Key);
        Assert.Equal("Room number", field.Name);
        Assert.True(field.IsRequired);
        Assert.NotEmpty(field.RowVersion);

        Assert.Equal(
            1,
            await _fixture.CountRowsIgnoringSecurityAsync(
                "[org].[Field]",
                $"[OrganizationId] = '{TestData.OrganizationA.Id}'"));
    }

    [Fact]
    public async Task First_active_field_in_a_scope_becomes_primary()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto first = await tenant.CreateTextFieldAsync("room_number");
        FieldDto second = await tenant.CreateTextFieldAsync("floor");

        Assert.True(first.IsPrimaryDisplayField);
        Assert.False(second.IsPrimaryDisplayField);
    }

    [Fact]
    public async Task Get_by_id_and_list_return_the_stored_field()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto created = await tenant.CreateTextFieldAsync("room_number");

        FieldDto fetched = await tenant.GetAsync(created.Id);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Key, fetched.Key);
        Assert.Equal(created.RowVersion, fetched.RowVersion);

        PagedResult<FieldDto> page = await tenant.ListAsync(new GetFieldsRequest
        {
            TargetEntityType = TestData.TargetEntityType.Room
        });

        FieldDto listed = Assert.Single(page.Items);
        Assert.Equal(created.Id, listed.Id);
        Assert.Equal(1, page.TotalItems);
    }

    [Fact]
    public async Task List_filters_by_target_entity_type()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        await tenant.CreateTextFieldAsync("room_number");
        await tenant.CreateTextFieldAsync(
            "nickname",
            TestData.TargetEntityType.Person);

        PagedResult<FieldDto> rooms = await tenant.ListAsync(new GetFieldsRequest
        {
            TargetEntityType = TestData.TargetEntityType.Room
        });

        Assert.Equal("room_number", Assert.Single(rooms.Items).Key);
    }

    [Fact]
    public async Task Paging_is_stable_when_the_sort_columns_are_identical()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        // Same DisplayOrder and same Name across every row, so only the [Id]
        // tie-breaker can make the order deterministic.
        for (int index = 0; index < 7; index++)
        {
            await tenant.CreateTextFieldAsync(
                $"attribute_{index}",
                name: "Attribute",
                displayOrder: 0);
        }

        List<Guid> firstPass = await ReadEveryPageAsync(tenant);
        List<Guid> secondPass = await ReadEveryPageAsync(tenant);

        // No row is skipped and none appears twice, and repeating the same
        // requests yields the same order, which is what the [Id] tie-breaker buys.
        Assert.Equal(7, firstPass.Count);
        Assert.Equal(7, firstPass.Distinct().Count());
        Assert.Equal(firstPass, secondPass);
    }

    private static async Task<List<Guid>> ReadEveryPageAsync(TenantScope tenant)
    {
        var ids = new List<Guid>();

        for (int page = 1; page <= 3; page++)
        {
            PagedResult<FieldDto> result = await tenant.ListAsync(new GetFieldsRequest
            {
                TargetEntityType = TestData.TargetEntityType.Room,
                Page = page,
                PageSize = 3
            });

            Assert.Equal(7, result.TotalItems);
            Assert.Equal(3, result.TotalPages);
            ids.AddRange(result.Items.Select(field => field.Id));
        }

        return ids;
    }

    [Fact]
    public async Task Page_size_is_capped_at_one_hundred()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        await tenant.CreateTextFieldAsync("room_number");

        PagedResult<FieldDto> page = await tenant.ListAsync(new GetFieldsRequest
        {
            PageSize = 5000
        });

        Assert.Equal(100, page.PageSize);
    }

    [Fact]
    public async Task Update_with_the_current_row_version_succeeds()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateTextFieldAsync("room_number");

        FieldDto updated = await tenant.UpdateAsync(
            field.Id,
            field.ToRenameRequest("Room no."));

        Assert.Equal("Room no.", updated.Name);
        Assert.NotEqual(field.RowVersion, updated.RowVersion);
        Assert.Equal("Room no.", (await tenant.GetAsync(field.Id)).Name);
    }

    [Fact]
    public async Task Update_with_a_stale_row_version_conflicts()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateTextFieldAsync("room_number");
        await tenant.UpdateAsync(field.Id, field.ToRenameRequest("Room no."));

        ConcurrencyConflictException exception =
            await Assert.ThrowsAsync<ConcurrencyConflictException>(
                () => tenant.UpdateAsync(
                    field.Id,
                    field.ToRenameRequest("Room number again")));

        Assert.Equal(MessageCode.Error.ConcurrencyConflict, exception.MessageKey);
        Assert.Equal("Room no.", (await tenant.GetAsync(field.Id)).Name);
    }

    [Fact]
    public async Task Delete_with_a_stale_row_version_conflicts()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateTextFieldAsync("room_number");
        await tenant.UpdateAsync(field.Id, field.ToRenameRequest("Room no."));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => tenant.DeleteAsync(
                field.Id,
                new DeleteFieldRequest { RowVersion = field.RowVersion }));

        Assert.NotNull(await tenant.GetAsync(field.Id));
    }

    [Fact]
    public async Task Soft_delete_keeps_the_row_and_hides_it_from_every_query()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateTextFieldAsync("room_number");

        await tenant.DeleteAsync(
            field.Id,
            new DeleteFieldRequest { RowVersion = field.RowVersion });

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => tenant.GetAsync(field.Id));

        PagedResult<FieldDto> page = await tenant.ListAsync(new GetFieldsRequest
        {
            TargetEntityType = TestData.TargetEntityType.Room
        });

        Assert.Empty(page.Items);

        // The row is still there, which is what keeps the key reserved.
        Assert.Equal(
            1,
            await _fixture.CountRowsIgnoringSecurityAsync(
                "[org].[Field]",
                $"[Id] = '{field.Id}' AND [DeletedAt] IS NOT NULL"));
    }

    [Fact]
    public async Task Soft_deleted_key_cannot_be_reused()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateTextFieldAsync("room_number");

        await tenant.DeleteAsync(
            field.Id,
            new DeleteFieldRequest { RowVersion = field.RowVersion });

        DuplicateResourceException exception =
            await Assert.ThrowsAsync<DuplicateResourceException>(
                () => tenant.CreateTextFieldAsync("ROOM_NUMBER"));

        Assert.Equal(MessageCode.Error.AlreadyExists, exception.MessageKey);
    }

    [Fact]
    public async Task Key_is_immutable_after_create()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateTextFieldAsync("room_number");

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => tenant.UpdateAsync(field.Id, new UpdateFieldRequest
                {
                    Name = field.Name,
                    Key = "room_no",
                    IsPrimaryDisplayField = true,
                    RowVersion = field.RowVersion
                }));

        Assert.Equal(MessageCode.Error.ImmutableProperty, exception.MessageKey);
    }

    [Fact]
    public async Task Field_type_is_immutable_after_create()
    {
        await _fixture.ResetOrgDataAsync();
        await using TenantScope tenant = OrganizationA();

        FieldDto field = await tenant.CreateTextFieldAsync("room_number");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => tenant.UpdateAsync(field.Id, new UpdateFieldRequest
            {
                Name = field.Name,
                FieldTypeId = (int)EFieldType.Number,
                IsPrimaryDisplayField = true,
                RowVersion = field.RowVersion
            }));
    }

    private TenantScope OrganizationA()
    {
        return TenantScope.For(
            _fixture,
            TestData.OrganizationA.Id,
            TestData.Users.AdministratorAId);
    }
}
