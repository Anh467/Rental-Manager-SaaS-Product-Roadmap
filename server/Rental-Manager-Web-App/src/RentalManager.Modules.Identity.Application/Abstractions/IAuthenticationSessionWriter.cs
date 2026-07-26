namespace RentalManager.Modules.Identity.Application.Abstractions;

public interface IAuthenticationSessionWriter
{
    Task WriteAsync(
        AuthenticationSession session,
        CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
