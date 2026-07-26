using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Contracts;
using RentalManager.Api.Security;
using RentalManager.BuildingBlocks.Tenancy.Services;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Authorization;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo;
using RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Org;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;
using RentalManager.Modules.TenantManagement.Domain.Entities.Dbo;
using DboRolePermissionRepository = RentalManager.Modules.TenantManagement.Application.Abstractions.Persistence.Dbo.IRolePermissionRepository;

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
    private readonly IOrganizationUserRepository _organizationUsers;
    private readonly OrganizationContextAccessor _organizationContextAccessor;
    private readonly JwtTokenIssuer _tokenIssuer;
    private readonly IRoleRepository _roles;
    private readonly DboRolePermissionRepository _globalRolePermissions;

    public AuthController(
        IUserRepository users,
        IPermissionReader permissionReader,
        IOrganizationUserRepository organizationUsers,
        OrganizationContextAccessor organizationContextAccessor,
        JwtTokenIssuer tokenIssuer,
        IRoleRepository roles,
        DboRolePermissionRepository globalRolePermissions)
    {
        _users = users;
        _permissionReader = permissionReader;
        _organizationUsers = organizationUsers;
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

        User user = await AuthenticateUserAsync(
            request.Email,
            request.Password,
            cancellationToken);

        return Ok(await IssueOrganizationTokenAsync(
            user,
            organizationId,
            cancellationToken));
    }

    /// <summary>
    /// Signs in with email and password only. Organization is resolved
    /// automatically: global admins get a global token; organization users with
    /// exactly one membership get that organization; multiple memberships require
    /// an explicit <see cref="TokenRequest.OrganizationId"/> via /token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> LoginAsync(
        [FromBody] TokenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationFailedException(
                nameof(TokenRequest.Email),
                MessageCode.Error.ValidationFailed);
        }

        User user = await AuthenticateUserAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (request.OrganizationId is Guid organizationId &&
            organizationId != Guid.Empty)
        {
            return Ok(await IssueOrganizationTokenAsync(
                user,
                organizationId,
                cancellationToken));
        }

        if (user.GlobalRoleId is not null)
        {
            (string token, DateTimeOffset expiresAt) = _tokenIssuer.IssueGlobal(user.Id);
            return Ok(ApiResponse<TokenResponse>.Create(
                new TokenResponse(token, "Bearer", expiresAt),
                MessageCode.Success.SignedIn,
                correlationId: HttpContext.TraceIdentifier));
        }

        IReadOnlyList<ActiveOrganizationMembership> memberships =
            await _organizationUsers.ListActiveMembershipsByUserIdAsync(
                user.Id,
                cancellationToken);

        if (memberships.Count == 0)
        {
            throw new AuthenticationFailedException();
        }

        if (memberships.Count > 1)
        {
            throw new ValidationFailedException(
                nameof(TokenRequest.OrganizationId),
                MessageCode.Error.OrganizationContextMissing);
        }

        return Ok(await IssueOrganizationTokenAsync(
            user,
            memberships[0].OrganizationId,
            cancellationToken));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> MeAsync(
        CancellationToken cancellationToken)
    {
        if (_organizationContextAccessor.UserId is not Guid userId)
        {
            throw new AuthenticationFailedException();
        }

        User user = await _users.GetAsync(userId, cancellationToken)
            ?? throw new AuthenticationFailedException();

        bool global = HttpContext.User.FindFirst(JwtClaimNames.Scope)?.Value == "global";
        if (global)
        {
            if (user.GlobalRoleId is not Guid roleId)
            {
                throw new AuthenticationFailedException();
            }

            Role role = await _roles.GetAsync(roleId, cancellationToken)
                ?? throw new AuthenticationFailedException();
            IReadOnlySet<string> permissions =
                await _globalRolePermissions.GetPermissionKeysByRoleAsync(
                    roleId,
                    cancellationToken);

            return Ok(ApiResponse<CurrentUserResponse>.Create(
                new CurrentUserResponse(
                    user.Id,
                    user.DisplayName,
                    user.Email,
                    "global",
                    null,
                    new { key = role.Key, name = role.Name },
                    permissions.ToArray()),
                MessageCode.Success.Retrieved,
                correlationId: HttpContext.TraceIdentifier));
        }

        IReadOnlySet<string> orgPermissions =
            await _permissionReader.GetPermissionKeysAsync(userId, cancellationToken);

        return Ok(ApiResponse<CurrentUserResponse>.Create(
            new CurrentUserResponse(
                user.Id,
                user.DisplayName,
                user.Email,
                "organization",
                _organizationContextAccessor.OrganizationId,
                null,
                orgPermissions.ToArray()),
            MessageCode.Success.Retrieved,
            correlationId: HttpContext.TraceIdentifier));
    }

    private async Task<User> AuthenticateUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        User user = await _users.FindByNormalizedEmailAsync(
            email.Trim().ToUpperInvariant(),
            cancellationToken)
            ?? throw new AuthenticationFailedException();

        if (!user.IsActive ||
            !PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
        {
            throw new AuthenticationFailedException();
        }

        return user;
    }

    private async Task<ApiResponse<TokenResponse>> IssueOrganizationTokenAsync(
        User user,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
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

        return ApiResponse<TokenResponse>.Create(
            new TokenResponse(token, "Bearer", expiresAt),
            MessageCode.Success.SignedIn,
            correlationId: HttpContext.TraceIdentifier);
    }
}
