namespace RentalManager.Modules.Identity.Application.Abstractions;

public sealed class CredentialAuthenticationResult
{
    private CredentialAuthenticationResult(
        bool succeeded,
        AuthenticatedIdentity? identity)
    {
        Succeeded = succeeded;
        Identity = identity;
    }

    public bool Succeeded { get; }

    public AuthenticatedIdentity? Identity { get; }

    public static CredentialAuthenticationResult Success(AuthenticatedIdentity identity) =>
        new(succeeded: true, identity);

    public static CredentialAuthenticationResult Failed() =>
        new(succeeded: false, identity: null);
}
