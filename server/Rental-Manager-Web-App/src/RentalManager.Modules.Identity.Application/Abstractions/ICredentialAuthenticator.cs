namespace RentalManager.Modules.Identity.Application.Abstractions;

public interface ICredentialAuthenticator
{
    Task<CredentialAuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedIdentity?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
