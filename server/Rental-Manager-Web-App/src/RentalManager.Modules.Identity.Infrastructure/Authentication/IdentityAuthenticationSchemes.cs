namespace RentalManager.Modules.Identity.Infrastructure.Authentication;

public static class IdentityAuthenticationSchemes
{
    /// <summary>
    /// The application session cookie. It carries this application's own user id
    /// and organization scope, never provider tokens.
    /// </summary>
    public const string ApplicationCookie = "RentalManager.Application";

    /// <summary>
    /// Short-lived cookie the provider handler signs into after a successful
    /// authorization response. It only exists long enough for the login endpoint
    /// to read the verified subject and exchange it for an application session.
    /// </summary>
    public const string ExternalCookie = "RentalManager.External";
}
