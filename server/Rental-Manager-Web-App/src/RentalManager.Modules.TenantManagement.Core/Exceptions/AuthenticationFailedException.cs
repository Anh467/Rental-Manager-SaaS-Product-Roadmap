using RentalManager.BuildingBlocks.Contracts.Messaging;

namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// The caller could not be authenticated. Deliberately carries no detail about
/// which part of the credentials was wrong. Maps to HTTP 401.
/// </summary>
public sealed class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException()
        : base(MessageCode.Error.AuthenticationRequired)
    {
    }
}
