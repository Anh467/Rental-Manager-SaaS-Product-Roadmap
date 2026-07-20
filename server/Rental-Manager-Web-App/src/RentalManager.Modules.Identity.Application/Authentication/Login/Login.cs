using RentalManager.Modules.Identity.Application.Abstractions;

namespace RentalManager.Modules.Identity.Application.Authentication.Login;

public sealed record LoginCommand(
    string Username,
    string Password);

public sealed record LoginResult(
    Guid UserId,
    string Username,
    string? Email,
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string? RefreshToken);

public sealed class LoginCommandHandler
{
    private readonly IAuthenticationProvider _authenticationProvider;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IAuthenticationProvider authenticationProvider,
        ITokenService tokenService)
    {
        _authenticationProvider = authenticationProvider;
        _tokenService = tokenService;
    }

    public async Task<LoginResult> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Username);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Password);

        AuthenticatedIdentity identity =
            await _authenticationProvider.AuthenticateAsync(
                command.Username,
                command.Password,
                cancellationToken)
            ?? throw new UnauthorizedAccessException(
                "The supplied credentials are invalid.");

        TokenResult token = await _tokenService.CreateAsync(
            identity,
            cancellationToken);

        return new LoginResult(
            identity.UserId,
            identity.Username,
            identity.Email,
            token.AccessToken,
            token.ExpiresAt,
            token.RefreshToken);
    }
}
