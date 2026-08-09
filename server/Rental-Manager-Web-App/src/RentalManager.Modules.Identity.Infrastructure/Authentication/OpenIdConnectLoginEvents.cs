using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Abstractions;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.Identity.Infrastructure.Options;

namespace RentalManager.Modules.Identity.Infrastructure.Authentication;

/// <summary>
/// Completes the external sign-in after the provider redirects back: the
/// verified principal is read, mapped onto a platform user, and the application
/// cookie is issued. Organization selection cannot be answered as JSON from the
/// callback, so that case keeps the short-lived external cookie and sends the
/// browser to the SPA path that finishes the exchange through POST /login.
/// </summary>
internal sealed class OpenIdConnectLoginEvents : OpenIdConnectEvents
{
    private const string SubjectClaim = "sub";

    private readonly ExternalAuthenticationOptions _options;

    public OpenIdConnectLoginEvents(ExternalAuthenticationOptions options)
    {
        _options = options;
    }

    public override Task TokenValidated(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? subject = context.Principal?.FindFirstValue(SubjectClaim)
            ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(subject))
        {
            context.Fail("The identity provider did not return a subject claim.");
        }

        return Task.CompletedTask;
    }

    public override async Task TicketReceived(TicketReceivedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Principal is null)
        {
            context.Fail("The identity provider did not return a principal.");
            return;
        }

        // Persist the verified principal so the shared resolver can read it the
        // same way the Development and test hosts do.
        await context.HttpContext.SignInAsync(
            IdentityAuthenticationSchemes.ExternalCookie,
            context.Principal,
            context.Properties);

        await using AsyncServiceScope scope =
            context.HttpContext.RequestServices.CreateAsyncScope();

        ICommandHandler<ExternalLoginCommand, LoginResult> login =
            scope.ServiceProvider
                .GetRequiredService<ICommandHandler<ExternalLoginCommand, LoginResult>>();

        LoginResult result;
        try
        {
            result = await login.HandleAsync(
                new ExternalLoginCommand(
                    Provider: null,
                    Subject: null,
                    Email: null,
                    DisplayName: null),
                context.HttpContext.RequestAborted);
        }
        catch
        {
            await context.HttpContext.SignOutAsync(
                IdentityAuthenticationSchemes.ExternalCookie);
            throw;
        }

        if (result.Status == LoginStatus.Authenticated)
        {
            await context.HttpContext.SignOutAsync(
                IdentityAuthenticationSchemes.ExternalCookie);

            context.HandleResponse();
            context.Response.Redirect(ResolveAuthenticatedRedirect(context));
            return;
        }

        if (result.Status == LoginStatus.OrganizationSelectionRequired)
        {
            // The SPA still needs the organization-selection JSON envelope. Leave
            // the external cookie in place and let POST /login finish the exchange.
            context.HandleResponse();
            context.Response.Redirect(ResolvePostLoginRedirect(context));
            return;
        }

        await context.HttpContext.SignOutAsync(
            IdentityAuthenticationSchemes.ExternalCookie);

        context.HandleResponse();
        context.Response.Redirect("/login");
    }

    private string ResolveAuthenticatedRedirect(TicketReceivedContext context)
    {
        string? redirect = context.Properties?.RedirectUri;
        if (IsLocalPath(redirect))
        {
            return redirect!;
        }

        return IsLocalPath(_options.PostLoginRedirectPath)
            ? _options.PostLoginRedirectPath
            : "/";
    }

    private string ResolvePostLoginRedirect(TicketReceivedContext context)
    {
        string? redirect = context.Properties?.RedirectUri;
        if (IsLocalPath(redirect))
        {
            return redirect!;
        }

        return IsLocalPath(_options.PostLoginRedirectPath)
            ? _options.PostLoginRedirectPath
            : "/login/callback";
    }

    private static bool IsLocalPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value == "/")
        {
            return true;
        }

        return value.Length >= 2
            && value[0] == '/'
            && value[1] != '/'
            && value[1] != '\\';
    }
}
