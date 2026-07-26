# Local development

The bootstrap administrator is disabled by default. For local development only,
set its credentials with user secrets; do not place a password in `appsettings.json`.

```bash
dotnet user-secrets set "BootstrapAdmin:Email" "<your-admin-email>"
dotnet user-secrets set "BootstrapAdmin:Password" "<your-strong-password>"
dotnet user-secrets set "BootstrapAdmin:Enabled" "true"
```

Run the commands from the `RentalManager.Api` project directory, or pass
`--project` pointing at `RentalManager.Api.csproj`.
