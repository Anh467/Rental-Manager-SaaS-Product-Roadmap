namespace RentalManager.Modules.Identity.Application.Abstractions;

/// <summary>
/// Produces the external identity the current request has actually been
/// verified as. The implementation owns where that verification comes from (the
/// provider handler's principal, or a development-only request payload when the
/// deployment explicitly enables it), which keeps that decision out of both the
/// controller and the use case.
/// </summary>
public interface IExternalIdentityResolver
{
    /// <summary>
    /// Returns the verified identity, or <c>null</c> when the request carries
    /// none. <paramref name="requestSupplied"/> is only ever honoured by a
    /// deployment that has opted into request-supplied identities.
    /// </summary>
    Task<ExternalIdentityDescriptor?> ResolveAsync(
        ExternalIdentityDescriptor? requestSupplied,
        CancellationToken cancellationToken = default);
}
