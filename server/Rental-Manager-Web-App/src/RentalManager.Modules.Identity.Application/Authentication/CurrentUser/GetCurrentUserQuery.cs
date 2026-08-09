using RentalManager.BuildingBlocks.Tenancy.Cqrs;
using RentalManager.Modules.Identity.Application.Contracts;

namespace RentalManager.Modules.Identity.Application.Authentication.CurrentUser;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserDto>;
