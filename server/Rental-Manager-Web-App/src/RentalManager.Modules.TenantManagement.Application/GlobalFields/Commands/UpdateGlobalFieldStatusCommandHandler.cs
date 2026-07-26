using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Application.Models.Dtos;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;

public sealed class UpdateGlobalFieldStatusCommandHandler(
    IFieldRepository fields,
    IGlobalFieldOptionRepository options)
    : ICommandHandler<UpdateGlobalFieldStatusCommand, FieldDto>
{
    public async Task<FieldDto> HandleAsync(
        UpdateGlobalFieldStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        var field = await fields.GetAsync(command.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);
        field.IsActive = command.Request.IsActive;
        await fields.UpdateAsync(
            field,
            FieldValidator.ParseRowVersion(command.Request.RowVersion),
            cancellationToken);
        return GlobalFieldDtoMapper.ToDto(
            field,
            await options.GetByFieldIdAsync(command.Id, cancellationToken));
    }
}
