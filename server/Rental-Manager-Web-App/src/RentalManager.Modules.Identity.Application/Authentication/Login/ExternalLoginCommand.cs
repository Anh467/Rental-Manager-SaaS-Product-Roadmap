using RentalManager.BuildingBlocks.Tenancy.Cqrs;

namespace RentalManager.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Maps an already-verified external identity onto a platform user and issues a
/// session. The caller is responsible for having verified the identity: this
/// command never accepts a credential and there is nothing here to verify one
/// with.
/// </summary>
public sealed record ExternalLoginCommand(
    string? Provider,
    string? Subject,
    string? Email,
    string? DisplayName) : ICommand<LoginResult>;
