using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RentalManager.Modules.TenantManagement.Infrastructure.IntegrationTests;

/// <summary>
/// Stands in for the provider handler. The identity is taken from request
/// headers and presented as a verified principal, so the tests drive the real
/// external-login path without a network call to an identity provider.
/// </summary>
internal sealed class TestExternalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestExternal";

    public const string SubjectHeader = "X-Test-Subject";
    public const string EmailHeader = "X-Test-Email";
    public const string DisplayNameHeader = "X-Test-DisplayName";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? subject = ReadHeader(SubjectHeader);

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new("sub", subject)
        };

        AddOptionalClaim(claims, ClaimTypes.Email, ReadHeader(EmailHeader));
        AddOptionalClaim(claims, "name", ReadHeader(DisplayNameHeader));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));

        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(principal, SchemeName)));
    }

    private string? ReadHeader(string name) =>
        Request.Headers.TryGetValue(name, out Microsoft.Extensions.Primitives.StringValues values)
            ? values.ToString()
            : null;

    private static void AddOptionalClaim(List<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(type, value));
        }
    }
}
