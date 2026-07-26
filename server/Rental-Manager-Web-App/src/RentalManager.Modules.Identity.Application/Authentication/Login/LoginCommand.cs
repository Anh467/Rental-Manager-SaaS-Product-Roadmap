using RentalManager.BuildingBlocks.Tenancy.Cqrs;

namespace RentalManager.Modules.Identity.Application.Authentication.Login;

public sealed record LoginCommand(string? Email, string? Password) : ICommand<LoginResult>;
