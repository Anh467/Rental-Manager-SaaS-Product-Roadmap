using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Authentication.Login;

namespace RentalManager.Modules.Identity.Application.Authentication.SelectOrganization;

public sealed record SelectOrganizationCommand(
    string? SelectionTicket,
    Guid? OrganizationId) : ICommand<LoginResult>;
