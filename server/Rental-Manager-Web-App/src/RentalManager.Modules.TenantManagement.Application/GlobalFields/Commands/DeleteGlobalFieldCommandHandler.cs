using RentalManager.Modules.TenantManagement.Application.Abstractions.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Fields;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.TenantManagement.Application.GlobalFields.Commands;

public sealed class DeleteGlobalFieldCommandHandler(
    ISqlSession session,
    IFieldRepository fields,
    IGlobalFieldOptionRepository options)
    : ICommandHandler<DeleteGlobalFieldCommand>
{
    public async Task HandleAsync(
        DeleteGlobalFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        FieldValidator.ValidateDelete(command.Request);
        await using var transaction = await session.BeginTransactionAsync(cancellationToken);
        _ = await fields.GetAsync(command.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);
        await fields.SoftDeleteAsync(
            command.Id,
            FieldValidator.ParseRowVersion(command.Request.RowVersion),
            cancellationToken);
        foreach (var option in await options.GetByFieldIdAsync(command.Id, cancellationToken))
        {
            await options.RetireAsync(option.Id, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
