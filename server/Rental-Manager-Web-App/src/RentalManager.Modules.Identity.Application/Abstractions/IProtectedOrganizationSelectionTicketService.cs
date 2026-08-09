namespace RentalManager.Modules.Identity.Application.Abstractions;

public interface IProtectedOrganizationSelectionTicketService
{
    string Protect(OrganizationSelectionTicket ticket);

    OrganizationSelectionTicket? Unprotect(string protectedTicket);
}
