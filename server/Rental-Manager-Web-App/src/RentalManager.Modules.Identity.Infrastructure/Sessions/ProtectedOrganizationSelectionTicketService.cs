using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using RentalManager.Modules.Identity.Application.Abstractions;

namespace RentalManager.Modules.Identity.Infrastructure.Sessions;

public sealed class ProtectedOrganizationSelectionTicketService
    : IProtectedOrganizationSelectionTicketService
{
    private const string ProtectorPurpose =
        "RentalManager.Identity.OrganizationSelectionTicket.v1";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDataProtector _protector;

    public ProtectedOrganizationSelectionTicketService(
        IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    }

    public string Protect(OrganizationSelectionTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        string payload = JsonSerializer.Serialize(ticket, SerializerOptions);
        return _protector.Protect(payload);
    }

    public OrganizationSelectionTicket? Unprotect(string protectedTicket)
    {
        if (string.IsNullOrWhiteSpace(protectedTicket))
        {
            return null;
        }

        try
        {
            string payload = _protector.Unprotect(protectedTicket);
            return JsonSerializer.Deserialize<OrganizationSelectionTicket>(
                payload,
                SerializerOptions);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
