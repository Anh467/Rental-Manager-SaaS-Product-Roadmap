using RentalManager.Modules.TenantManagement.Application.Fields.Commands;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Application.UnitTests.Fakes;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Core.Enums;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class CreateFieldCommandHandlerTests
{
    private readonly FakeOrgFieldRepository _fields = new();
    private readonly FakeFieldOptionRepository _options = new();
    private readonly FakeFieldTypeRepository _fieldTypes = new();
    private readonly FakeSqlSession _session = new();
    private readonly CreateFieldCommandHandler _create;
    private readonly UpdateFieldCommandHandler _update;
    private readonly DeleteFieldCommandHandler _delete;

    public CreateFieldCommandHandlerTests()
    {
        var optionSynchronizer = new OrgFieldOptionSynchronizer(_options);

        _create = new CreateFieldCommandHandler(
            _session,
            _fields,
            _fieldTypes,
            optionSynchronizer);

        _update = new UpdateFieldCommandHandler(
            _session,
            _fields,
            optionSynchronizer);

        _delete = new DeleteFieldCommandHandler(
            _session,
            _fields,
            _options);
    }

    [Fact]
    public async Task Create_stores_the_key_and_trimmed_values()
    {
        FieldDto field = await _create.HandleAsync(new CreateFieldCommand(
            new CreateFieldRequest
            {
                Key = "room_number",
                Name = "  Room number  ",
                Description = "   ",
                FieldTypeId = (int)EFieldType.Text
            }));

        Assert.Equal("room_number", field.Key);
        Assert.Equal("Room number", field.Name);
        Assert.Null(field.Description);
        Assert.Equal("room_number", _fields.Require(field.Id).Key);
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
    public async Task Duplicate_key_is_rejected_with_the_already_exists_code()
    {
        await CreateTextFieldAsync("room_number");

        DomainException exception = await Assert.ThrowsAsync<DuplicateResourceException>(
            () => CreateTextFieldAsync("room_number"));

        Assert.Equal(MessageCode.Error.AlreadyExists, exception.MessageKey);
    }

    [Fact]
    public async Task Soft_deleted_key_cannot_be_reused()
    {
        FieldDto first = await CreateTextFieldAsync("room_number");

        await _delete.HandleAsync(new DeleteFieldCommand(
            first.Id,
            new DeleteFieldRequest { RowVersion = first.RowVersion }));

        await Assert.ThrowsAsync<DuplicateResourceException>(
            () => CreateTextFieldAsync("room_number"));
    }

    [Fact]
    public async Task Unknown_field_type_fails_validation()
    {
        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _create.HandleAsync(new CreateFieldCommand(
                    new CreateFieldRequest
                    {
                        Key = "moved_in_on",
                        Name = "Moved in on",
                        FieldTypeId = 99
                    })));

        Assert.Equal(MessageCode.Error.ValidationFailed, exception.MessageKey);
        Assert.Contains(
            exception.Failures,
            failure => failure.MessageKey == MessageCode.Error.FieldTypeMismatch);
    }

    [Fact]
    public async Task Missing_catalogue_row_reports_the_type_mismatch_code()
    {
        _fieldTypes.Clear();

        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => CreateTextFieldAsync("room_number"));

        Assert.Contains(
            exception.Failures,
            failure =>
                failure.FieldKey == nameof(CreateFieldRequest.FieldTypeId) &&
                failure.MessageKey == MessageCode.Error.FieldTypeMismatch);
    }

    [Fact]
    public async Task Options_are_rejected_for_a_non_option_field()
    {
        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _create.HandleAsync(new CreateFieldCommand(
                    new CreateFieldRequest
                    {
                        Key = "room_number",
                        Name = "Room number",
                        FieldTypeId = (int)EFieldType.Text,
                        Options = [new FieldOptionInput { Key = "a", Name = "A" }]
                    })));

        Assert.Contains(
            exception.Failures,
            failure => failure.MessageKey == MessageCode.Error.InvalidFieldOption);
    }

    [Theory]
    [InlineData(EFieldType.Selection)]
    [InlineData(EFieldType.MultiSelect)]
    public async Task Option_field_without_options_is_rejected(EFieldType fieldType)
    {
        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _create.HandleAsync(new CreateFieldCommand(
                    new CreateFieldRequest
                    {
                        Key = "amenities",
                        Name = "Amenities",
                        FieldTypeId = (int)fieldType
                    })));

        Assert.Contains(
            exception.Failures,
            failure => failure.MessageKey == MessageCode.Error.InvalidFieldOption);
    }

    [Fact]
    public async Task Duplicate_option_keys_are_rejected()
    {
        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _create.HandleAsync(new CreateFieldCommand(
                    new CreateFieldRequest
                    {
                        Key = "amenities",
                        Name = "Amenities",
                        FieldTypeId = (int)EFieldType.MultiSelect,
                        Options =
                        [
                            new FieldOptionInput { Key = "wifi", Name = "Wi-Fi" },
                            new FieldOptionInput { Key = "wifi", Name = "Wifi again" }
                        ]
                    })));

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

        FieldDto updated = await _update.HandleAsync(new UpdateFieldCommand(
            field.Id,
            new UpdateFieldRequest
            {
                Name = "Room no.",
                RowVersion = field.RowVersion
            }));

        Assert.Equal("Room no.", updated.Name);
        Assert.NotEqual(field.RowVersion, updated.RowVersion);
    }

    [Fact]
    public async Task Update_with_a_stale_row_version_conflicts()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        await _update.HandleAsync(new UpdateFieldCommand(
            field.Id,
            new UpdateFieldRequest
            {
                Name = "Room no.",
                RowVersion = field.RowVersion
            }));

        ConcurrencyConflictException exception =
            await Assert.ThrowsAsync<ConcurrencyConflictException>(
                () => _update.HandleAsync(new UpdateFieldCommand(
                    field.Id,
                    new UpdateFieldRequest
                    {
                        Name = "Room number again",
                        RowVersion = field.RowVersion
                    })));

        Assert.Equal(MessageCode.Error.ConcurrencyConflict, exception.MessageKey);
    }

    [Fact]
    public async Task Update_rejects_a_changed_key()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => _update.HandleAsync(new UpdateFieldCommand(
                    field.Id,
                    new UpdateFieldRequest
                    {
                        Name = "Room number",
                        Key = "room_no",
                        RowVersion = field.RowVersion
                    })));

        Assert.Equal(MessageCode.Error.ImmutableProperty, exception.MessageKey);
        Assert.Equal(
            nameof(Field.Key),
            exception.Parameters[MessageCode.Parameter.Field]);
    }

    [Fact]
    public async Task Update_accepts_the_unchanged_key()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        FieldDto updated = await _update.HandleAsync(new UpdateFieldCommand(
            field.Id,
            new UpdateFieldRequest
            {
                Name = "Room number",
                Key = "room_number",
                RowVersion = field.RowVersion
            }));

        Assert.Equal("room_number", updated.Key);
    }

    [Fact]
    public async Task Update_rejects_a_changed_field_type()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        BusinessRuleException exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => _update.HandleAsync(new UpdateFieldCommand(
                    field.Id,
                    new UpdateFieldRequest
                    {
                        Name = "Room number",
                        FieldTypeId = (int)EFieldType.Number,
                        RowVersion = field.RowVersion
                    })));

        Assert.Equal(MessageCode.Error.ImmutableProperty, exception.MessageKey);
        Assert.Equal(
            nameof(Field.FieldTypeId),
            exception.Parameters[MessageCode.Parameter.Field]);
    }

    [Fact]
    public async Task Delete_with_a_stale_row_version_conflicts()
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        await _update.HandleAsync(new UpdateFieldCommand(
            field.Id,
            new UpdateFieldRequest
            {
                Name = "Room no.",
                RowVersion = field.RowVersion
            }));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _delete.HandleAsync(new DeleteFieldCommand(
                field.Id,
                new DeleteFieldRequest { RowVersion = field.RowVersion })));
    }

    [Fact]
    public async Task Deleting_a_field_retires_its_options()
    {
        FieldDto field = await CreateMultiSelectFieldAsync();

        await _delete.HandleAsync(new DeleteFieldCommand(
            field.Id,
            new DeleteFieldRequest { RowVersion = field.RowVersion }));

        Assert.NotNull(_fields.Require(field.Id).DeletedAt);
        Assert.All(_options.All, option => Assert.NotNull(option.DeletedAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base64!!")]
    public async Task Update_requires_a_valid_row_version_token(string? rowVersion)
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _update.HandleAsync(new UpdateFieldCommand(
                    field.Id,
                    new UpdateFieldRequest
                    {
                        Name = "Room no.",
                        RowVersion = rowVersion
                    })));

        Assert.Equal(MessageCode.Error.ValidationFailed, exception.MessageKey);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    public async Task Update_rejects_row_version_tokens_that_are_not_exactly_eight_bytes(
        int byteLength)
    {
        FieldDto field = await CreateTextFieldAsync("room_number");
        string rowVersion = Convert.ToBase64String(new byte[byteLength]);

        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _update.HandleAsync(new UpdateFieldCommand(
                    field.Id,
                    new UpdateFieldRequest
                    {
                        Name = "Room no.",
                        RowVersion = rowVersion
                    })));

        Assert.Equal(MessageCode.Error.ValidationFailed, exception.MessageKey);
        Assert.Contains(
            exception.Failures,
            failure =>
                failure.FieldKey == nameof(UpdateFieldRequest.RowVersion) &&
                failure.MessageKey == MessageCode.Error.ValidationFailed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AAAA")]
    public async Task Delete_requires_a_valid_eight_byte_row_version(string? rowVersion)
    {
        FieldDto field = await CreateTextFieldAsync("room_number");

        ValidationFailedException exception =
            await Assert.ThrowsAsync<ValidationFailedException>(
                () => _delete.HandleAsync(new DeleteFieldCommand(
                    field.Id,
                    new DeleteFieldRequest { RowVersion = rowVersion })));

        Assert.Equal(MessageCode.Error.ValidationFailed, exception.MessageKey);
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
    public async Task Missing_field_reports_not_found_on_update()
    {
        ResourceNotFoundException exception =
            await Assert.ThrowsAsync<ResourceNotFoundException>(
                () => _update.HandleAsync(new UpdateFieldCommand(
                    Guid.NewGuid(),
                    new UpdateFieldRequest
                    {
                        Name = "Missing",
                        RowVersion = Convert.ToBase64String(new byte[8])
                    })));

        Assert.Equal(MessageCode.Error.NotFound, exception.MessageKey);
    }

    private Task<FieldDto> CreateTextFieldAsync(string key)
    {
        return _create.HandleAsync(new CreateFieldCommand(new CreateFieldRequest
        {
            Key = key,
            Name = key,
            FieldTypeId = (int)EFieldType.Text
        }));
    }

    private Task<FieldDto> CreateMultiSelectFieldAsync()
    {
        return _create.HandleAsync(new CreateFieldCommand(new CreateFieldRequest
        {
            Key = "amenities",
            Name = "Amenities",
            FieldTypeId = (int)EFieldType.MultiSelect,
            Options =
            [
                new FieldOptionInput { Key = "parking", Name = "Parking" },
                new FieldOptionInput { Key = "wifi", Name = "Wi-Fi", DisplayOrder = 1 }
            ]
        }));
    }
}
