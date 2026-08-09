namespace RentalManager.BuildingBlocks.Contracts.Security;

/// <summary>
/// One security-relevant occurrence, described with safe structured data.
/// <see cref="Data"/> must only ever contain non-sensitive values: identifiers,
/// stable provider keys, scopes and safe reason codes. Tokens, secrets,
/// passwords, password hashes, authorization headers, raw provider payloads and
/// stack traces must never be placed here.
/// </summary>
public sealed record SecurityEvent(
    string EventType,
    string CorrelationId,
    IReadOnlyDictionary<string, object?> Data)
{
    public static SecurityEvent Create(
        string eventType,
        string? correlationId,
        IReadOnlyDictionary<string, object?>? data = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        return new SecurityEvent(
            eventType,
            string.IsNullOrWhiteSpace(correlationId) ? UnknownCorrelationId : correlationId,
            data ?? new Dictionary<string, object?>(0));
    }

    /// <summary>
    /// Used when a security event is raised outside a correlated request, so
    /// the field is still always present for downstream consumers.
    /// </summary>
    public const string UnknownCorrelationId = "unknown";
}

/// <summary>
/// Canonical security event type names. Consumers (SCRUM-84 owns durable
/// persistence and querying) depend on these strings, so they are treated the
/// same way as message keys: additive, never silently renamed.
/// </summary>
public static class SecurityEventTypes
{
    public const string LoginSucceeded = "LoginSucceeded";

    public const string LoginFailed = "LoginFailed";

    public const string Logout = "Logout";

    public const string IdentityConflict = "IdentityConflict";

    public const string InactiveUserRejected = "InactiveUserRejected";

    public const string UserProvisioned = "UserProvisioned";
}

/// <summary>
/// Field names used inside <see cref="SecurityEvent.Data"/>, so producers and
/// consumers agree without duplicating string literals.
/// </summary>
public static class SecurityEventFields
{
    public const string UserId = "userId";

    public const string Provider = "provider";

    public const string Scope = "scope";

    public const string OrganizationId = "organizationId";

    public const string Reason = "reason";

    public const string Object = "object";
}

/// <summary>
/// Safe, stable reason codes for rejected security operations. These are not
/// message keys and are never rendered to end users.
/// </summary>
public static class SecurityEventReasons
{
    public const string ProviderNotAllowed = "providerNotAllowed";

    public const string SubjectMissing = "subjectMissing";

    public const string EmailMissing = "emailMissing";

    public const string EmailAlreadyLinked = "emailAlreadyLinked";

    public const string UserInactive = "userInactive";

    public const string NoActiveMembership = "noActiveMembership";

    public const string ExternalPrincipalMissing = "externalPrincipalMissing";
}
