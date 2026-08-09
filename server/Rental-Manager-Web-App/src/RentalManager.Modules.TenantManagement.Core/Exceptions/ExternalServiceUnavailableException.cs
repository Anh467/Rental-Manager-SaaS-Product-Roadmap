using RentalManager.BuildingBlocks.Contracts.Messaging;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// An external dependency (for example object storage) could not service the
/// request. <paramref name="service"/> must be a safe, static identifier such
/// as <c>objectStorage</c> — never a URL, connection string or provider error
/// message. Maps to HTTP 503.
/// </summary>
public sealed class ExternalServiceUnavailableException : DomainException
{
    public ExternalServiceUnavailableException(string service, Exception? innerException = null)
        : base(
            MessageCode.Error.ServiceUnavailable,
            new Dictionary<string, object?>
            {
                [MessageCode.Parameter.Service] = service
            },
            innerException)
    {
    }
}
