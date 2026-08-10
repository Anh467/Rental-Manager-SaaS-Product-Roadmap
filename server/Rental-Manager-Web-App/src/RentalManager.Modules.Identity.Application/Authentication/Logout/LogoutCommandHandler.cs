using RentalManager.BuildingBlocks.Contracts.Security;
using RentalManager.BuildingBlocks.Tenancy.Abstractions;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;

namespace RentalManager.Modules.Identity.Application.Authentication.Logout;

public sealed class LogoutCommandHandler(
    IAuthenticationSessionWriter sessionWriter,
    ICurrentIdentity currentIdentity,
    ISecurityEventPublisher securityEvents,
    IOrganizationContext organizationContext)
    : ICommandHandler<LogoutCommand>
{
    public async Task HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Guid? userId = currentIdentity.UserId;

        await sessionWriter.ClearAsync(cancellationToken);

        var data = new Dictionary<string, object?>();
        if (userId is Guid id)
        {
            data[SecurityEventFields.UserId] = id;
        }

        await securityEvents.PublishAsync(
            SecurityEvent.Create(
                SecurityEventTypes.Logout,
                organizationContext.CorrelationId,
                data),
            cancellationToken);
    }
}
