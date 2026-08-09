using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Common;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Org;

namespace RentalManager.Modules.TenantManagement.Application.Fields.Commands;

public sealed class DeleteFieldCommandHandler : ICommandHandler<DeleteFieldCommand>
{
    private readonly ISqlSession _session;
    private readonly IOrgFieldRepository _fields;
    private readonly IFieldOptionRepository _fieldOptions;

    public DeleteFieldCommandHandler(
        ISqlSession session,
        IOrgFieldRepository fields,
        IFieldOptionRepository fieldOptions)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(fieldOptions);

        _session = session;
        _fields = fields;
        _fieldOptions = fieldOptions;
    }

    public async Task HandleAsync(
        DeleteFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        FieldValidator.ValidateDelete(command.Request);
        byte[] expectedRowVersion = FieldValidator.ParseRowVersion(command.Request.RowVersion);

        await using ISqlTransactionScope transaction =
            await _session.BeginTransactionAsync(cancellationToken);

        _ = await _fields.GetAsync(command.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(FieldInvariants.ObjectName);

        await _fields.SoftDeleteAsync(command.Id, expectedRowVersion, cancellationToken);

        foreach (FieldOption option in
                 await _fieldOptions.GetByFieldIdAsync(command.Id, cancellationToken))
        {
            await _fieldOptions.RetireAsync(option.Id, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
