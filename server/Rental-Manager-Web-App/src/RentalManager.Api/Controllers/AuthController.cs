using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RentalManager.BuildingBlocks.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Authentication.CurrentUser;
using RentalManager.Modules.Identity.Application.Authentication.Login;
using RentalManager.Modules.Identity.Application.Authentication.Logout;
using RentalManager.Modules.Identity.Application.Authentication.SelectOrganization;
using RentalManager.Modules.Identity.Application.Contracts;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Api.Controllers;

public sealed record LoginRequest(string? Email, string? Password);

public sealed record SelectOrganizationRequest(
    string? SelectionTicket,
    Guid? OrganizationId);

public sealed record CsrfTokenResponse(string RequestToken);

public sealed record OrganizationSelectionResponse(
    string Status,
    IReadOnlyList<OrganizationOptionDto> Organizations,
    string SelectionTicket);

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    ICommandHandler<LoginCommand, LoginResult> login,
    ICommandHandler<SelectOrganizationCommand, LoginResult> selectOrganization,
    ICommandHandler<LogoutCommand> logout,
    IQueryHandler<GetCurrentUserQuery, CurrentUserDto> getCurrentUser,
    IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult<ApiResponse<CsrfTokenResponse>> GetCsrfToken()
    {
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Headers.CacheControl = "no-store";

        return Ok(ApiResponse<CsrfTokenResponse>.Create(
            new CsrfTokenResponse(tokens.RequestToken ?? string.Empty),
            MessageCode.Success.Retrieved,
            correlationId: HttpContext.TraceIdentifier));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<object>>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        LoginResult result = await login.HandleAsync(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        return MapLoginResult(result);
    }

    [HttpPost("select-organization")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<object>>> SelectOrganizationAsync(
        [FromBody] SelectOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        LoginResult result = await selectOrganization.HandleAsync(
            new SelectOrganizationCommand(
                request.SelectionTicket,
                request.OrganizationId),
            cancellationToken);

        return MapLoginResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> LogoutAsync(
        CancellationToken cancellationToken)
    {
        await logout.HandleAsync(new LogoutCommand(), cancellationToken);

        return Ok(ApiResponse<object>.Create(
            null,
            MessageCode.Success.SignedIn,
            correlationId: HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> MeAsync(
        CancellationToken cancellationToken)
    {
        CurrentUserDto user = await getCurrentUser.HandleAsync(
            new GetCurrentUserQuery(),
            cancellationToken);

        return Ok(ApiResponse<CurrentUserDto>.Create(
            user,
            MessageCode.Success.Retrieved,
            correlationId: HttpContext.TraceIdentifier));
    }

    private ActionResult<ApiResponse<object>> MapLoginResult(LoginResult result)
    {
        return result.Status switch
        {
            LoginStatus.Failed => throw new AuthenticationFailedException(),
            LoginStatus.Authenticated => Ok(ApiResponse<object>.Create(
                result.Authentication,
                MessageCode.Success.SignedIn,
                correlationId: HttpContext.TraceIdentifier)),
            LoginStatus.OrganizationSelectionRequired => Ok(ApiResponse<object>.Create(
                new OrganizationSelectionResponse(
                    "organizationSelectionRequired",
                    result.Organizations ?? [],
                    result.SelectionTicket ?? string.Empty),
                MessageCode.Success.SignedIn,
                correlationId: HttpContext.TraceIdentifier)),
            _ => throw new AuthenticationFailedException()
        };
    }
}
