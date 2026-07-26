# Local development

## Connection string

The API reads `ConnectionStrings:RentalManager` (fallback: `DefaultConnection`).
Update `appsettings.Development.json`, or override with user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:RentalManager" "Server=MSI\\SQLEXPRESS;Database=RentalManager.Database.SQLServer_1;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

Publish the SQL Database project to that database before starting the API.

## Bootstrap administrator

Disabled by default. For local development only, set credentials with user secrets;
do not place a password in `appsettings.json`.

```bash
dotnet user-secrets set "BootstrapAdmin:Email" "<your-admin-email>"
dotnet user-secrets set "BootstrapAdmin:Password" "<your-strong-password>"
dotnet user-secrets set "BootstrapAdmin:Enabled" "true"
```

Run the commands from the `RentalManager.Api` project directory, or pass
`--project` pointing at `RentalManager.Api.csproj`.

## Frontend (Vite) proxy

The Vite app proxies `/api` to `http://localhost:5008`. In Development the API
does **not** redirect HTTP → HTTPS, because that redirect drops the
`Authorization` header and makes `/auth/me` return `ERR-003` after login.

Prefer the `http` launch profile (`http://localhost:5008`) when using the Vite proxy.
