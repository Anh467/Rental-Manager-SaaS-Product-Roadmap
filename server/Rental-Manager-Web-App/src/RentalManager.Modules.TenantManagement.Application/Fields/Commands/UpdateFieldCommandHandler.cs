using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Commands;

public sealed class UpdateFieldCommandHandler : ICommandHandler<UpdateFieldCommand, FieldDto>
{
    private readonly ISqlSession _session;
    private readonly IOrgFieldRepository _fields;
    private readonly OrgFieldOptionSynchronizer _optionSynchronizer;

    public UpdateFieldCommandHandler(
        ISqlSession session,
        IOrgFieldRepository fields,
        OrgFieldOptionSynchronizer optionSynchronizer)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(optionSynchronizer);

        _session = session;
        _fields = fields;
        _optionSynchronizer = optionSynchronizer;
    }

    public async Task<FieldDto> HandleAsync(
        UpdateFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        UpdateFieldRequest request = command.Request;
        ArgumentNullException.ThrowIfNull(request);

        byte[] expectedRowVersion = FieldValidator.ParseRowVersion(request.RowVersion);

        await using ISqlTransactionScope transaction =
            await _session.BeginTransactionAsync(cancellationToken);

        Field field = await _fields.GetAsync(command.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);

        FieldValidator.ValidateUpdate(request, field.FieldTypeId);
        EnsureImmutablePropertiesUnchanged(field, request);

        field.Name = request.Name!.Trim();
        field.Description = FieldDtoMapper.NormalizeDescription(request.Description);
        field.IsActive = request.IsActive;

        await _fields.UpdateAsync(field, expectedRowVersion, cancellationToken);

        IReadOnlyList<FieldOption> options = await _optionSynchronizer.ReplaceAsync(
            field,
            request.Options,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return FieldDtoMapper.ToDto(field, options);
    }

    private static void EnsureImmutablePropertiesUnchanged(
        Field field,
        UpdateFieldRequest request)
    {
        if (request.Key is not null &&
            !string.Equals(request.Key, field.Key, StringComparison.Ordinal))
        {
            throw new BusinessRuleException(
                MessageCode.Error.ImmutableProperty,
                new Dictionary<string, object?>
                {
                    [MessageCode.Parameter.Field] = nameof(Field.Key)
                });
        }

        if (request.FieldTypeId is int requestedFieldTypeId &&
            requestedFieldTypeId != field.FieldTypeId)
        {
            throw new BusinessRuleException(
                MessageCode.Error.ImmutableProperty,
                new Dictionary<string, object?>
                {
                    [MessageCode.Parameter.Field] = nameof(Field.FieldTypeId)
                });
        }
    }
}
