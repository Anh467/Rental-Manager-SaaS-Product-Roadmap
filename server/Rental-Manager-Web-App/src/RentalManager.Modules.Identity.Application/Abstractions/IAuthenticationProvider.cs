namespace RentalManager.Modules.Identity.Application.Abstractions;

public sealed record AuthenticatedIdentity(
    Guid UserId,
    string Username,
    string? Email);

public interface IAuthenticationProvider
{
    Task<AuthenticatedIdentity?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken);
}
