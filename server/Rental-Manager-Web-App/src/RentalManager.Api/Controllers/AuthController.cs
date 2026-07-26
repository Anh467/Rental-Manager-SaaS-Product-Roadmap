using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Contracts;
using RentalManager.Api.Security;
using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;

namespace RentalManager.Api.Controllers;

public sealed record TokenRequest(
    string? Email,
    string? Password,
    Guid? OrganizationId);

public sealed record TokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);

public sealed record CurrentUserResponse(
    Guid Id,
    string Name,
    string Email,
    string Scope,
    Guid? OrganizationId,
    object? Role,
    IReadOnlyCollection<string> Permissions);

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPermissionReader _permissionReader;
    private readonly OrganizationContextAccessor _organizationContextAccessor;
    private readonly JwtTokenIssuer _tokenIssuer;
    private readonly IRoleRepository _roles;
    private readonly IRolePermissionRepository _globalRolePermissions;

    public AuthController(
        IUserRepository users,
        IPermissionReader permissionReader,
        OrganizationContextAccessor organizationContextAccessor,
        JwtTokenIssuer tokenIssuer,
        IRoleRepository roles,
        IRolePermissionRepository globalRolePermissions)
    {
        _users = users;
        _permissionReader = permissionReader;
        _organizationContextAccessor = organizationContextAccessor;
        _tokenIssuer = tokenIssuer;
        _roles = roles;
        _globalRolePermissions = globalRolePermissions;
    }

    /// <summary>
    /// Exchanges credentials plus a chosen organization for a token bound to that
    /// organization. Membership is resolved from
    /// <c>[org].[OrganizationUser]</c>, so one user may belong to many
    /// organizations and pick which one to enter.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> IssueTokenAsync(
        [FromBody] TokenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.OrganizationId is not Guid organizationId ||
            organizationId == Guid.Empty)
        {
            throw new ValidationFailedException(
                nameof(TokenRequest.Email),
                MessageCode.Error.ValidationFailed);
        }

        User user = await _users.FindByNormalizedEmailAsync(
            request.Email.Trim().ToUpperInvariant(),
            cancellationToken)
            ?? throw new AuthenticationFailedException();

        if (!user.IsActive ||
            !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new AuthenticationFailedException();
        }

        // Bind the organization before the membership query so RLS only reveals
        // a membership row for an organization the user actually belongs to.
        _organizationContextAccessor.SetOrganization(organizationId);
        _organizationContextAccessor.SetUser(user.Id);

        bool isMember = await _permissionReader.IsActiveMemberAsync(
            user.Id,
            cancellationToken);

        if (!isMember)
        {
            throw new MissingOrganizationContextException();
        }

        (string token, DateTimeOffset expiresAt) = _tokenIssuer.Issue(
            user.Id,
            organizationId);

        return Ok(ApiResponse<TokenResponse>.Create(
            new TokenResponse(token, "Bearer", expiresAt),
            MessageCode.Success.SignedIn));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> LoginAsync(
        [FromBody] TokenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OrganizationId is not null)
        {
            return await IssueTokenAsync(request, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationFailedException(nameof(TokenRequest.Email), MessageCode.Error.ValidationFailed);
        }

        User user = await _users.FindByNormalizedEmailAsync(
            request.Email.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new AuthenticationFailedException();
        if (!user.IsActive || user.GlobalRoleId is null ||
            !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new AuthenticationFailedException();
        }

        (string token, DateTimeOffset expiresAt) = _tokenIssuer.IssueGlobal(user.Id);
        return Ok(ApiResponse<TokenResponse>.Create(
            new TokenResponse(token, "Bearer", expiresAt),
            MessageCode.Success.SignedIn,
            correlationId: HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> MeAsync(CancellationToken cancellationToken)
    {
        if (_organizationContextAccessor.UserId is not Guid userId)
            throw new AuthenticationFailedException();
        User user = await _users.GetAsync(userId, cancellationToken) ?? throw new AuthenticationFailedException();
        bool global = HttpContext.User.FindFirst(JwtClaimNames.Scope)?.Value == "global";
        if (global)
        {
            if (user.GlobalRoleId is not Guid roleId) throw new AuthenticationFailedException();
            var role = await _roles.GetAsync(roleId, cancellationToken) ?? throw new AuthenticationFailedException();
            var permissions = await _globalRolePermissions.GetPermissionKeysByRoleAsync(roleId, cancellationToken);
            return Ok(ApiResponse<CurrentUserResponse>.Create(new CurrentUserResponse(
                user.Id, user.DisplayName, user.Email, "global", null,
                new { key = role.Key, name = role.Name }, permissions.ToArray()),
                MessageCode.Success.Retrieved, correlationId: HttpContext.TraceIdentifier));
        }

        var orgPermissions = await _permissionReader.GetPermissionKeysAsync(userId, cancellationToken);
        return Ok(ApiResponse<CurrentUserResponse>.Create(new CurrentUserResponse(
            user.Id, user.DisplayName, user.Email, "organization",
            _organizationContextAccessor.OrganizationId, null, orgPermissions.ToArray()),
            MessageCode.Success.Retrieved, correlationId: HttpContext.TraceIdentifier));
    }
}
