# Local development

## Connection string

The API reads `ConnectionStrings:RentalManager` (fallback: `DefaultConnection`).
Update `appsettings.Development.json`, or override with user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:RentalManager" "Server=MSI\\SQLEXPRESS;Database=RentalManager.Database.SQLServer_1;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

Publish the SQL Database project to that database before starting the API.

## Cookie authentication

Authentication uses ASP.NET Core Identity application cookies (HttpOnly), not JWT.

- Development cookie name: `rentalmanager.auth` (`Secure` = same-as-request)
- Production cookie name: `__Host-rentalmanager.auth` (`Secure` = always)
- Frontend must call APIs with `credentials: include` / Axios `withCredentials: true`
- CSRF: `GET /api/v1/auth/csrf`, then send `X-CSRF-TOKEN` on POST/PUT/PATCH/DELETE

Prefer the `http` launch profile (`http://localhost:5008`) with the Vite proxy.

## Data Protection (production)

Cookie auth and organization-selection tickets use ASP.NET Core Data Protection.

Set a durable key-ring path outside the repo and outside ephemeral container storage:

```bash
# Example only — use your secret store / mounted volume in production.
dotnet user-secrets set "DataProtection:KeyRingPath" "D:\\secure\\rental-manager\\dp-keys"
```

Do not commit key material. In CI/tests the host sets a temporary key-ring path.

## Bootstrap administrator

Disabled by default. For local development only, set credentials with user secrets;
do not place a password in `appsettings.json`. Placeholders such as
`your-admin@email.com` / `YourStrongPassword` are rejected.

```bash
dotnet user-secrets set "BootstrapAdmin:Email" "<your-admin-email>"
dotnet user-secrets set "BootstrapAdmin:Password" "<your-strong-password>"
dotnet user-secrets set "BootstrapAdmin:Enabled" "true"
```

Run the commands from the `RentalManager.Api` project directory, or pass
`--project` pointing at `RentalManager.Api.csproj`.
