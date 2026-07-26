using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;

public sealed class UpdateGlobalFieldCommandHandler(
    ISqlSession session,
    IFieldRepository fields,
    GlobalFieldOptionSynchronizer optionSynchronizer)
    : ICommandHandler<UpdateGlobalFieldCommand, FieldDto>
{
    public async Task<FieldDto> HandleAsync(
        UpdateGlobalFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        UpdateFieldRequest request = command.Request;
        ArgumentNullException.ThrowIfNull(request);

        Field field = await fields.GetAsync(command.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);
        FieldValidator.ValidateUpdate(request, field.FieldTypeId);

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

        field.Name = request.Name!.Trim();
        field.Description = GlobalFieldDtoMapper.Clean(request.Description);
        field.IsActive = request.IsActive;

        await using var transaction = await session.BeginTransactionAsync(cancellationToken);
        await fields.UpdateAsync(
            field,
            FieldValidator.ParseRowVersion(request.RowVersion),
            cancellationToken);
        var replaced = await optionSynchronizer.ReplaceAsync(
            field,
            request.Options,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return GlobalFieldDtoMapper.ToDto(field, replaced);
    }
}
