using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;

namespace RentalManager.Modules.Identity.Application.Authentication.Logout;

public sealed class LogoutCommandHandler(
    IAuthenticationSessionWriter sessionWriter)
    : ICommandHandler<LogoutCommand>
{
    public Task HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return sessionWriter.ClearAsync(cancellationToken);
    }
}
