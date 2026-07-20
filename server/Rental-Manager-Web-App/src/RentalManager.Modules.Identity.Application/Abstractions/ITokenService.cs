namespace RentalManager.Modules.Identity.Application.Abstractions;

public sealed record TokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string? RefreshToken = null);

public interface ITokenService
{
    Task<TokenResult> CreateAsync(
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken);
}
