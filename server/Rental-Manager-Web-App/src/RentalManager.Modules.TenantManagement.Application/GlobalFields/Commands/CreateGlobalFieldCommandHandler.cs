using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;

public sealed class CreateGlobalFieldCommandHandler(
    ISqlSession session,
    IFieldRepository fields,
    IFieldTypeRepository fieldTypes,
    GlobalFieldOptionSynchronizer optionSynchronizer)
    : ICommandHandler<CreateGlobalFieldCommand, FieldDto>
{
    public async Task<FieldDto> HandleAsync(
        CreateGlobalFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        CreateFieldRequest request = command.Request;
        ArgumentNullException.ThrowIfNull(request);

        FieldValidator.ValidateCreate(request);
        if (await fieldTypes.GetAsync(request.FieldTypeId, cancellationToken) is null)
        {
            throw new ValidationFailedException(
                nameof(CreateFieldRequest.FieldTypeId),
                MessageCode.Error.FieldTypeMismatch);
        }

        await using var transaction = await session.BeginTransactionAsync(cancellationToken);
        if (await fields.FindByKeyAsync(request.Key!, cancellationToken) is not null)
        {
            throw new DuplicateResourceException(FieldInvariants.ObjectName, "key");
        }

        var field = new Field
        {
            Id = Guid.CreateVersion7(),
            Key = request.Key!,
            Name = request.Name!.Trim(),
            Description = GlobalFieldDtoMapper.Clean(request.Description),
            FieldTypeId = request.FieldTypeId,
            IsActive = request.IsActive
        };

        await fields.InsertAsync(field, cancellationToken);
        var created = await optionSynchronizer.ReplaceAsync(
            field,
            request.Options,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return GlobalFieldDtoMapper.ToDto(field, created);
    }
}
