using RentalManager.Modules.TenantManagement.Application.Abstractions.Auditing;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Application.UnitTests.Fakes;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class FieldServiceTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid UserId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    private const string Target = "Room";

    private readonly FakeOrgFieldRepository _fields = new();
    private readonly FakeFieldOptionRepository _options = new();
    private readonly FakeSqlSession _session = new();
    private readonly FakeSqlApplicationLock _applicationLock = new();
    private readonly FakeAuditLogWriter _auditLog = new();
    private readonly FieldService _service;

    public FieldServiceTests()
    {
        _service = new FieldService(
            _session,
            _applicationLock,
            _fields,
            _options,
            _auditLog,
            new FakeOrganizationContext(OrganizationId, UserId));
    }

    [Fact]
    public async Task Create_stores_the_normalized_key_and_trimmed_values()
    {
        FieldDto field = await _service.CreateFieldAsync(new CreateFieldRequest
        {
            TargetEntityType = Target,
            Key = "  room_number  ",
            Name = "  Room number  ",
            Description = "   ",
            FieldTypeId = (int)EFieldType.Text
        });

        Assert.Equal("room_number", field.Key);
        Assert.Equal("Room number", field.Name);
        Assert.Null(field.Description);
        Assert.Equal("ROOM_NUMBER", _fields.Require(field.Id).NormalizedKey);
    }

    [Fact]
    public async Task Create_opens_and_commits_exactly_one_transaction()
    {
        await CreateTextFieldAsync("room_number");

        Assert.Equal(1, _session.BeganTransactions);
        Assert.Equal(1, _session.CommittedTransactions);
        Assert.Equal(0, _session.RolledBackTransactions);
    }

    [Fact]
    public async Task Create_takes_the_scope_lock_before_writing()
    {
        await CreateTextFieldAsync("room_number");

        Assert.Equal(
            [FieldInvariants.LockResourceName(OrganizationId, Target)],
            _applicationLock.AcquiredResources);
    }

    [Fact]
    public async Task First_active_field_in_a_scope_becomes_primary()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        Assert.True(field.IsPrimaryDisplayField);
    }

    [Fact]
    public async Task Second_field_does_not_become_primary_unless_asked()
    {
        await CreateTextFieldAsync("room_number");
        FieldDto second = await CreateTextFieldAsync("floor");

        Assert.False(second.IsPrimaryDisplayField);
    }

    [Fact]
    public async Task Requesting_primary_on_create_demotes_the_incumbent()
    {
        FieldDto first = await CreateTextFieldAsync("room_number");

        FieldDto second = await _service.CreateFieldAsync(new CreateFieldRequest
        {
            TargetEntityType = Target,
            Key = "floor",
            Name = "Floor",
            FieldTypeId = (int)EFieldType.Text,
            IsPrimaryDisplayField = true
        });

        Assert.True(second.IsPrimaryDisplayField);
        Assert.False(_fields.Require(first.Id).IsPrimaryDisplayField);
    }

    [Fact]
    public async Task Inactive_field_never_becomes_primary()
    {
        FieldDto field = await _service.CreateFieldAsync(new CreateFieldRequest
        {
            TargetEntityType = Target,
            Key = "room_number",
            Name = "Room number",
            FieldTypeId = (int)EFieldType.Text,
            IsActive = false,
            IsPrimaryDisplayField = true
        });

        Assert.False(field.IsPrimaryDisplayField);
    }

    [Fact]
    public async Task Duplicate_key_is_rejected_with_the_already_exists_code()
    {
        await CreateTextFieldAsync("room_number");

        DomainException exception = await Assert.ThrowsAsync<DuplicateResourceException>(
            () => CreateTextFieldAsync("  ROOM_NUMBER "));

        Assert.Equal(MessageCode.Error.AlreadyExists, exception.MessageKey);
    }

    [Fact]
    public async Task Soft_deleted_key_cannot_be_reused()
    {
        FieldDto first = await CreateTextFieldAsync("room_number");
        await CreateTextFieldAsync("floor");

        await _service.DeleteFieldAsync(first.Id, new DeleteFieldRequest
        {
            RowVersion = first.RowVersion,
            ReplacementPrimaryFieldId = _fields.All
                .Single(field => field.NormalizedKey == "FLOOR").Id
        });

        await Assert.ThrowsAsync<DuplicateResourceException>(
            () => CreateTextFieldAsync("room_number"));
    }

    [Fact]
    public async Task Unsupported_target_entity_type_fails_validation()
    {
        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _service.CreateFieldAsync(new CreateFieldRequest
                {
                    TargetEntityType = "Invoice",
                    Key = "code",
                    Name = "Code",
                    FieldTypeId = (int)EFieldType.Text
                }));

        Assert.Contains(
            exception.Failures,
            failure => failure.FieldKey == nameof(CreateFieldRequest.TargetEntityType));
    }

    [Fact]
    public async Task Field_type_outside_the_mvp_reports_the_type_mismatch_code()
    {
        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _service.CreateFieldAsync(new CreateFieldRequest
                {
                    TargetEntityType = Target,
                    Key = "moved_in_on",
                    Name = "Moved in on",
                    FieldTypeId = (int)EFieldType.Date
                }));

        Assert.Contains(
            exception.Failures,
            failure => failure.MessageKey == MessageCode.Error.FieldTypeMismatch);
    }

    [Fact]
    public async Task Options_are_rejected_for_a_non_multi_select_field()
    {
        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _service.CreateFieldAsync(new CreateFieldRequest
                {
                    TargetEntityType = Target,
                    Key = "room_number",
                    Name = "Room number",
                    FieldTypeId = (int)EFieldType.Text,
                    Options = [new FieldOptionInput { Key = "a", Name = "A" }]
                }));

        Assert.Contains(
            exception.Failures,
            failure => failure.MessageKey == MessageCode.Error.InvalidFieldOption);
    }

    [Fact]
    public async Task Multi_select_without_options_is_rejected()
    {
        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _service.CreateFieldAsync(new CreateFieldRequest
                {
                    TargetEntityType = Target,
                    Key = "amenities",
                    Name = "Amenities",
                    FieldTypeId = (int)EFieldType.MultiSelect
                }));

        Assert.Contains(
            exception.Failures,
            failure => failure.MessageKey == MessageCode.Error.InvalidFieldOption);
    }

    [Fact]
    public async Task Duplicate_option_keys_are_rejected()
    {
        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _service.CreateFieldAsync(new CreateFieldRequest
                {
                    TargetEntityType = Target,
                    Key = "amenities",
                    Name = "Amenities",
                    FieldTypeId = (int)EFieldType.MultiSelect,
                    Options =
                    [
                        new FieldOptionInput { Key = "wifi", Name = "Wi-Fi" },
                        new FieldOptionInput { Key = " WIFI ", Name = "Wifi again" }
                    ]
                }));

        Assert.Contains(
            exception.Failures,
            failure => failure.MessageKey == MessageCode.Error.InvalidFieldOption);
    }

    [Fact]
    public async Task Multi_select_options_are_created_with_the_field()
    {
        FieldDto field = await CreateMultiSelectFieldAsync();

        Assert.Equal(2, field.Options.Count);
        Assert.Equal(["parking", "wifi"], field.Options.Select(option => option.Key));
        Assert.All(_options.All, option => Assert.Equal(field.Id, option.FieldId));
    }

    [Fact]
    public async Task Update_with_the_current_row_version_succeeds()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        FieldDto updated = await _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
        {
            Name = "Room no.",
            IsRequired = true,
            IsPrimaryDisplayField = true,
            RowVersion = field.RowVersion
        });

        Assert.Equal("Room no.", updated.Name);
        Assert.True(updated.IsRequired);
        Assert.NotEqual(field.RowVersion, updated.RowVersion);
    }

    [Fact]
    public async Task Update_with_a_stale_row_version_conflicts()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        await _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
        {
            Name = "Room no.",
            IsPrimaryDisplayField = true,
            RowVersion = field.RowVersion
        });

        ConcurrencyConflictException exception =
            await Assert.ThrowsAsync<ConcurrencyConflictException>(
                () => _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
                {
                    Name = "Room number again",
                    IsPrimaryDisplayField = true,
                    RowVersion = field.RowVersion
                }));

        Assert.Equal(MessageCode.Error.ConcurrencyConflict, exception.MessageKey);
    }

    [Fact]
    public async Task Update_rejects_a_changed_key()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
                {
                    Name = "Room number",
                    Key = "room_no",
                    IsPrimaryDisplayField = true,
                    RowVersion = field.RowVersion
                }));

        Assert.Equal(MessageCode.Error.ImmutableProperty, exception.MessageKey);
        Assert.Equal(
            nameof(Field.Key),
            exception.Parameters[MessageCode.Parameter.Field]);
    }

    [Fact]
    public async Task Update_accepts_the_unchanged_key_in_any_casing()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        FieldDto updated = await _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
        {
            Name = "Room number",
            Key = " ROOM_NUMBER ",
            IsPrimaryDisplayField = true,
            RowVersion = field.RowVersion
        });

        Assert.Equal("room_number", updated.Key);
    }

    [Fact]
    public async Task Update_rejects_a_changed_field_type()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
                {
                    Name = "Room number",
                    FieldTypeId = (int)EFieldType.Number,
                    IsPrimaryDisplayField = true,
                    RowVersion = field.RowVersion
                }));

        Assert.Equal(MessageCode.Error.ImmutableProperty, exception.MessageKey);
        Assert.Equal(
            nameof(Field.FieldTypeId),
            exception.Parameters[MessageCode.Parameter.Field]);
    }

    [Fact]
    public async Task Primary_moves_atomically_between_two_fields()
    {
        FieldDto first = await CreateTextFieldAsync("room_number");
        FieldDto second = await CreateTextFieldAsync("floor");

        FieldDto promoted = await _service.UpdateFieldAsync(
            second.Id,
            new UpdateFieldRequest
            {
                Name = second.Name,
                IsPrimaryDisplayField = true,
                RowVersion = second.RowVersion
            });

        Assert.True(promoted.IsPrimaryDisplayField);
        Assert.False(_fields.Require(first.Id).IsPrimaryDisplayField);
        Assert.Single(
            _fields.All,
            field => field is { IsPrimaryDisplayField: true, IsActive: true });
    }

    [Fact]
    public async Task Demoting_the_primary_requires_a_replacement()
    {
        FieldDto primary = await CreateTextFieldAsync("room_number");
        await CreateTextFieldAsync("floor");

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => _service.UpdateFieldAsync(primary.Id, new UpdateFieldRequest
                {
                    Name = primary.Name,
                    IsPrimaryDisplayField = false,
                    RowVersion = primary.RowVersion
                }));

        Assert.Equal(
            MessageCode.Error.LastActivePrimaryFieldRemoval,
            exception.MessageKey);
    }

    [Fact]
    public async Task Deactivating_the_primary_requires_a_replacement()
    {
        FieldDto primary = await CreateTextFieldAsync("room_number");
        await CreateTextFieldAsync("floor");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.UpdateFieldAsync(primary.Id, new UpdateFieldRequest
            {
                Name = primary.Name,
                IsActive = false,
                RowVersion = primary.RowVersion
            }));
    }

    [Fact]
    public async Task Deleting_the_primary_requires_a_replacement()
    {
        FieldDto primary = await CreateTextFieldAsync("room_number");
        await CreateTextFieldAsync("floor");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.DeleteFieldAsync(primary.Id, new DeleteFieldRequest
            {
                RowVersion = primary.RowVersion
            }));
    }

    [Fact]
    public async Task Replacement_must_belong_to_the_same_scope()
    {
        FieldDto primary = await CreateTextFieldAsync("room_number");
        await CreateTextFieldAsync("floor");

        FieldDto otherScope = await _service.CreateFieldAsync(new CreateFieldRequest
        {
            TargetEntityType = "Person",
            Key = "nickname",
            Name = "Nickname",
            FieldTypeId = (int)EFieldType.Text
        });

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.DeleteFieldAsync(primary.Id, new DeleteFieldRequest
            {
                RowVersion = primary.RowVersion,
                ReplacementPrimaryFieldId = otherScope.Id
            }));
    }

    [Fact]
    public async Task Deleting_the_primary_with_a_replacement_moves_the_flag()
    {
        FieldDto primary = await CreateTextFieldAsync("room_number");
        FieldDto replacement = await CreateTextFieldAsync("floor");

        await _service.DeleteFieldAsync(primary.Id, new DeleteFieldRequest
        {
            RowVersion = primary.RowVersion,
            ReplacementPrimaryFieldId = replacement.Id
        });

        Assert.NotNull(_fields.Require(primary.Id).DeletedAt);
        Assert.True(_fields.Require(replacement.Id).IsPrimaryDisplayField);
    }

    [Fact]
    public async Task Deleting_the_only_field_needs_no_replacement()
    {
        FieldDto only = await CreateTextFieldAsync("room_number");

        await _service.DeleteFieldAsync(only.Id, new DeleteFieldRequest
        {
            RowVersion = only.RowVersion
        });

        Assert.NotNull(_fields.Require(only.Id).DeletedAt);
    }

    [Fact]
    public async Task Delete_with_a_stale_row_version_conflicts()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        await _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
        {
            Name = "Room no.",
            IsPrimaryDisplayField = true,
            RowVersion = field.RowVersion
        });

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _service.DeleteFieldAsync(field.Id, new DeleteFieldRequest
            {
                RowVersion = field.RowVersion
            }));
    }

    [Fact]
    public async Task Deleting_a_field_retires_its_options()
    {
        FieldDto field = await CreateMultiSelectFieldAsync();

        await _service.DeleteFieldAsync(field.Id, new DeleteFieldRequest
        {
            RowVersion = field.RowVersion
        });

        Assert.All(_options.All, option => Assert.NotNull(option.DeletedAt));
    }

    [Fact]
    public async Task Removing_an_option_retires_it_and_keeps_the_rest()
    {
        FieldDto field = await CreateMultiSelectFieldAsync();

        FieldDto updated = await _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
        {
            Name = field.Name,
            IsPrimaryDisplayField = true,
            RowVersion = field.RowVersion,
            Options = [new FieldOptionInput { Key = "wifi", Name = "Wi-Fi" }]
        });

        Assert.Single(updated.Options);
        Assert.Equal("wifi", updated.Options[0].Key);
        Assert.Single(_options.All, option => option.DeletedAt is not null);
    }

    [Fact]
    public async Task Renaming_an_option_keeps_its_identity()
    {
        FieldDto field = await CreateMultiSelectFieldAsync();
        Guid wifiOptionId = field.Options.Single(option => option.Key == "wifi").Id;

        FieldDto updated = await _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
        {
            Name = field.Name,
            IsPrimaryDisplayField = true,
            RowVersion = field.RowVersion,
            Options =
            [
                new FieldOptionInput { Key = "wifi", Name = "Wireless internet" },
                new FieldOptionInput { Key = "parking", Name = "Parking", DisplayOrder = 1 }
            ]
        });

        FieldOptionDto wifi = updated.Options.Single(option => option.Key == "wifi");

        Assert.Equal(wifiOptionId, wifi.Id);
        Assert.Equal("Wireless internet", wifi.Name);
    }

    [Fact]
    public async Task Missing_field_reports_not_found()
    {
        ResourceNotFoundException exception =
            await Assert.ThrowsAsync<ResourceNotFoundException>(
                () => _service.GetFieldAsync(Guid.NewGuid()));

        Assert.Equal(MessageCode.Error.NotFound, exception.MessageKey);
    }

    [Fact]
    public async Task List_filters_by_target_entity_type_and_active_state()
    {
        await CreateTextFieldAsync("room_number");
        await _service.CreateFieldAsync(new CreateFieldRequest
        {
            TargetEntityType = "Person",
            Key = "nickname",
            Name = "Nickname",
            FieldTypeId = (int)EFieldType.Text
        });

        PagedResult<FieldDto> page = await _service.GetFieldsAsync(new GetFieldsRequest
        {
            TargetEntityType = Target,
            IsActive = true
        });

        Assert.Single(page.Items);
        Assert.Equal("room_number", page.Items[0].Key);
        Assert.Equal(1, page.TotalItems);
    }

    [Fact]
    public async Task List_rejects_an_unknown_target_entity_type()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(
            () => _service.GetFieldsAsync(new GetFieldsRequest
            {
                TargetEntityType = "Invoice"
            }));
    }

    [Fact]
    public async Task Every_write_use_case_writes_exactly_one_audit_entry()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        await _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
        {
            Name = "Room no.",
            IsPrimaryDisplayField = true,
            RowVersion = field.RowVersion
        });

        Field stored = _fields.Require(field.Id);

        await _service.DeleteFieldAsync(field.Id, new DeleteFieldRequest
        {
            RowVersion = Convert.ToBase64String(stored.RowVersion)
        });

        Assert.Equal(
            [AuditAction.Created, AuditAction.Updated, AuditAction.Deleted],
            _auditLog.Entries.Select(entry => entry.Action));

        Assert.All(
            _auditLog.Entries,
            entry => Assert.Equal(field.Id, entry.EntityId));
    }

    [Fact]
    public async Task Audit_summary_never_contains_the_raw_request()
    {
        FieldDto field = await _service.CreateFieldAsync(new CreateFieldRequest
        {
            TargetEntityType = Target,
            Key = "tenant_secret_key",
            Name = "Display label",
            Description = "Sensitive note about a tenant",
            FieldTypeId = (int)EFieldType.Text
        });

        AuditLogEntry entry = Assert.Single(_auditLog.Entries);

        Assert.NotNull(entry.ChangeSummary);
        Assert.Contains("Name=Display label", entry.ChangeSummary);
        Assert.DoesNotContain("tenant_secret_key", entry.ChangeSummary);
        Assert.DoesNotContain("Sensitive note", entry.ChangeSummary);
        Assert.DoesNotContain(Target, entry.ChangeSummary);
        Assert.Equal(field.Id, entry.EntityId);
    }

    [Fact]
    public async Task Failed_use_case_rolls_the_transaction_back()
    {
        await CreateTextFieldAsync("room_number");

        await Assert.ThrowsAsync<DuplicateResourceException>(
            () => CreateTextFieldAsync("room_number"));

        Assert.Equal(2, _session.BeganTransactions);
        Assert.Equal(1, _session.CommittedTransactions);
        Assert.Equal(1, _session.RolledBackTransactions);
    }

    [Fact]
    public async Task Missing_organization_context_fails_closed()
    {
        var service = new FieldService(
            _session,
            _applicationLock,
            _fields,
            _options,
            _auditLog,
            new FakeOrganizationContext(organizationId: null));

        await Assert.ThrowsAsync<MissingOrganizationContextException>(
            () => service.CreateFieldAsync(new CreateFieldRequest
            {
                TargetEntityType = Target,
                Key = "room_number",
                Name = "Room number",
                FieldTypeId = (int)EFieldType.Text
            }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base64!!")]
    public async Task Update_requires_a_valid_row_version_token(string? rowVersion)
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        await Assert.ThrowsAsync<ValidationFailedException>(
            () => _service.UpdateFieldAsync(field.Id, new UpdateFieldRequest
            {
                Name = "Room no.",
                RowVersion = rowVersion
            }));
    }

    private Task<FieldDto> CreateTextFieldAsync(string key)
    {
        return _service.CreateFieldAsync(new CreateFieldRequest
        {
            TargetEntityType = Target,
            Key = key,
            Name = key,
            FieldTypeId = (int)EFieldType.Text
        });
    }

    private Task<FieldDto> CreateMultiSelectFieldAsync()
    {
        return _service.CreateFieldAsync(new CreateFieldRequest
        {
            TargetEntityType = Target,
            Key = "amenities",
            Name = "Amenities",
            FieldTypeId = (int)EFieldType.MultiSelect,
            Options =
            [
                new FieldOptionInput { Key = "parking", Name = "Parking" },
                new FieldOptionInput { Key = "wifi", Name = "Wi-Fi", DisplayOrder = 1 }
            ]
        });
    }
}
