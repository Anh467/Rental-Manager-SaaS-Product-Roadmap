using Microsoft.Extensions.Options;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Infrastructure.Options;

namespace RentalManager.Modules.Identity.Infrastructure.Authentication;

public sealed class ExternalLoginChallengeFactory(
    IOptionsMonitor<ExternalAuthenticationOptions> options)
    : IExternalLoginChallengeFactory
{
    public bool IsConfigured => options.CurrentValue.IsOpenIdConnectConfigured;

    public ExternalLoginChallenge Create(string? returnUrl)
    {
        ExternalAuthenticationOptions current = options.CurrentValue;

        return new ExternalLoginChallenge(
            current.DefaultScheme,
            IsLocalPath(returnUrl) ? returnUrl! : current.PostLoginRedirectPath);
    }

    /// <summary>
    /// Only a single-slash relative path is accepted. <c>//host</c> and
    /// <c>/\host</c> are protocol-relative and would leave the site.
    /// </summary>
    private static bool IsLocalPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            return value == "/";
        }

        return value[0] == '/' && value[1] != '/' && value[1] != '\\';
    }
}
