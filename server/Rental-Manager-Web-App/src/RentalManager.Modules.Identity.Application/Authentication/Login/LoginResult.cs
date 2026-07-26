using RentalManager.Modules.Identity.Application.Contracts;

namespace RentalManager.Modules.Identity.Application.Authentication.Login;

public enum LoginStatus
{
    Failed = 0,
    Authenticated = 1,
    OrganizationSelectionRequired = 2
}

public sealed class LoginResult
{
    private LoginResult(
        LoginStatus status,
        AuthenticationResultDto? authentication,
        IReadOnlyList<OrganizationOptionDto>? organizations,
        string? selectionTicket)
    {
        Status = status;
        Authentication = authentication;
        Organizations = organizations;
        SelectionTicket = selectionTicket;
    }

    public LoginStatus Status { get; }

    public AuthenticationResultDto? Authentication { get; }

    public IReadOnlyList<OrganizationOptionDto>? Organizations { get; }

    public string? SelectionTicket { get; }

    public static LoginResult Failed() =>
        new(LoginStatus.Failed, null, null, null);

    public static LoginResult Authenticated(AuthenticationResultDto authentication) =>
        new(LoginStatus.Authenticated, authentication, null, null);

    public static LoginResult OrganizationSelectionRequired(
        IReadOnlyList<OrganizationOptionDto> organizations,
        string selectionTicket) =>
        new(
            LoginStatus.OrganizationSelectionRequired,
            null,
            organizations,
            selectionTicket);
}
