using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Commands;

public sealed class CreateFieldCommandHandler : ICommandHandler<CreateFieldCommand, FieldDto>
{
    private readonly ISqlSession _session;
    private readonly IOrgFieldRepository _fields;
    private readonly IFieldTypeRepository _fieldTypes;
    private readonly OrgFieldOptionSynchronizer _optionSynchronizer;

    public CreateFieldCommandHandler(
        ISqlSession session,
        IOrgFieldRepository fields,
        IFieldTypeRepository fieldTypes,
        OrgFieldOptionSynchronizer optionSynchronizer)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(fieldTypes);
        ArgumentNullException.ThrowIfNull(optionSynchronizer);

        _session = session;
        _fields = fields;
        _fieldTypes = fieldTypes;
        _optionSynchronizer = optionSynchronizer;
    }

    public async Task<FieldDto> HandleAsync(
        CreateFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        CreateFieldRequest request = command.Request;
        ArgumentNullException.ThrowIfNull(request);

        FieldValidator.ValidateCreate(request);
        if (await _fieldTypes.GetAsync(request.FieldTypeId, cancellationToken) is null)
        {
            throw new ValidationFailedException(
                nameof(CreateFieldRequest.FieldTypeId),
                MessageCode.Error.FieldTypeMismatch);
        }

        await using ISqlTransactionScope transaction =
            await _session.BeginTransactionAsync(cancellationToken);

        Field? conflicting = await _fields.FindByKeyAsync(
            request.Key!,
            cancellationToken);

        if (conflicting is not null)
        {
            throw new DuplicateResourceException(FieldInvariants.ObjectName);
        }

        var field = new Field
        {
            Id = Guid.CreateVersion7(),
            Key = request.Key!,
            Name = request.Name!.Trim(),
            Description = FieldDtoMapper.NormalizeDescription(request.Description),
            FieldTypeId = request.FieldTypeId,
            IsActive = request.IsActive
        };

        await _fields.InsertAsync(field, cancellationToken);

        IReadOnlyList<FieldOption> options = await _optionSynchronizer.ReplaceAsync(
            field,
            request.Options,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return FieldDtoMapper.ToDto(field, options);
    }
}
