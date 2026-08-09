using Microsoft.Data.SqlClient;

namespace RentalManager.Modules.Identity.Infrastructure.Persistence;

public interface IIdentityConnectionFactory
{
    Task<SqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default);
}
