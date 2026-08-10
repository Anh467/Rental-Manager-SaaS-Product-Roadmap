namespace RentalManager.BuildingBlocks.Contracts.Messaging;

/// <summary>
/// Whether a message represents a successful outcome or an error.
/// </summary>
public enum MessageKind
{
    Success,
    Error
}

/// <summary>
/// Whether a message key may still be emitted by the backend. Deprecated keys
/// are kept in the catalog only so historical responses and locale files stay
/// meaningful; new code must never emit them.
/// </summary>
public enum MessageLifecycle
{
    Active,
    Deprecated
}

/// <summary>
/// One immutable entry in the shared client/server message catalog. Keys are
/// never renumbered or reused: retiring a message moves it to
/// <see cref="MessageLifecycle.Deprecated"/> instead of deleting it.
/// </summary>
public sealed record MessageDefinition(
    string Key,
    MessageKind Kind,
    MessageLifecycle Lifecycle,
    string? ReplacementHint = null);

/// <summary>
/// The full canonical message catalog (Confluence 08.1: Message Catalog).
/// This is the single source of truth for which message keys exist, whether
/// they are success or error, and whether they are still active. The client
/// owns localized text for every key here; the backend must only ever emit
/// keys with <see cref="MessageLifecycle.Active"/>.
/// </summary>
public static class MessageCatalog
{
    public static IReadOnlyList<MessageDefinition> All { get; } = BuildCatalog();

    public static IReadOnlySet<string> ActiveKeys { get; } =
        All.Where(definition => definition.Lifecycle == MessageLifecycle.Active)
            .Select(definition => definition.Key)
            .ToHashSet(StringComparer.Ordinal);

    public static IReadOnlySet<string> DeprecatedKeys { get; } =
        All.Where(definition => definition.Lifecycle == MessageLifecycle.Deprecated)
            .Select(definition => definition.Key)
            .ToHashSet(StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, MessageDefinition> ByKey { get; } =
        All.ToDictionary(definition => definition.Key, StringComparer.Ordinal);

    public static bool IsActive(string key) => ActiveKeys.Contains(key);

    public static bool IsDeprecated(string key) => DeprecatedKeys.Contains(key);

    public static bool TryGet(string key, out MessageDefinition definition) =>
        ByKey.TryGetValue(key, out definition!);

    private static List<MessageDefinition> BuildCatalog()
    {
        var definitions = new List<MessageDefinition>();

        // Success: SCS-001 through SCS-020. All active; none have been
        // retired yet.
        for (int number = 1; number <= 20; number++)
        {
            definitions.Add(new MessageDefinition(
                FormatKey("SCS", number),
                MessageKind.Success,
                MessageLifecycle.Active));
        }

        // Error, active: ERR-001..026, ERR-028..033, ERR-040, ERR-041,
        // ERR-048, ERR-049, ERR-050.
        IEnumerable<int> activeErrorNumbers =
        [
            .. Enumerable.Range(1, 26),
            .. Enumerable.Range(28, 6),
            40, 41, 48, 49, 50
        ];

        foreach (int number in activeErrorNumbers)
        {
            definitions.Add(new MessageDefinition(
                FormatKey("ERR", number),
                MessageKind.Error,
                MessageLifecycle.Active));
        }

        // Error, deprecated: ERR-027, ERR-034..039, ERR-042..047. Kept for
        // historical/locale parity only; the backend must never emit these.
        IEnumerable<int> deprecatedErrorNumbers =
        [
            27,
            .. Enumerable.Range(34, 6),
            .. Enumerable.Range(42, 6)
        ];

        foreach (int number in deprecatedErrorNumbers)
        {
            definitions.Add(new MessageDefinition(
                FormatKey("ERR", number),
                MessageKind.Error,
                MessageLifecycle.Deprecated));
        }

        return definitions
            .OrderBy(definition => definition.Kind)
            .ThenBy(definition => definition.Key, StringComparer.Ordinal)
            .ToList();
    }

    private static string FormatKey(string prefix, int number) =>
        $"{prefix}-{number:D3}";
}

/// <summary>
/// Canonical message codes shared with the client. The client owns the
/// localized text, so the backend must only ever emit these codes. Every
/// constant here refers to an <see cref="MessageLifecycle.Active"/> entry in
/// <see cref="MessageCatalog"/>; deprecated keys are intentionally not
/// exposed as constants so the backend cannot accidentally emit them.
/// </summary>
public static class MessageCode
{
    public static class Success
    {
        public const string Created = "SCS-001";
        public const string Updated = "SCS-002";
        public const string Deactivated = "SCS-003";
        public const string Reactivated = "SCS-004";
        public const string Retrieved = "SCS-005";
        public const string Accepted = "SCS-006";
        public const string Published = "SCS-007";
        public const string Archived = "SCS-008";
        public const string Assigned = "SCS-009";
        public const string Unassigned = "SCS-010";
        public const string UploadAccepted = "SCS-011";
        public const string Linked = "SCS-012";
        public const string Unlinked = "SCS-013";
        public const string StateChanged = "SCS-014";
        public const string RetryQueued = "SCS-015";
        public const string IdempotentReplay = "SCS-016";
        public const string SignedIn = "SCS-017";
        public const string SignedOut = "SCS-018";
        public const string ConfigurationOverrideApplied = "SCS-019";
        public const string OrganizationProvisioned = "SCS-020";
    }

    public static class Error
    {
        public const string ValidationFailed = "ERR-001";
        public const string NotFound = "ERR-002";
        public const string AuthenticationRequired = "ERR-003";
        public const string PermissionDenied = "ERR-004";
        public const string OrganizationContextMissing = "ERR-005";
        public const string CrossOrganizationReference = "ERR-006";
        public const string AlreadyExists = "ERR-007";
        public const string ImmutableProperty = "ERR-008";
        public const string Inactive = "ERR-009";
        public const string ConcurrencyConflict = "ERR-010";
        public const string FieldRequired = "ERR-011";
        public const string FieldTypeMismatch = "ERR-012";
        public const string InvalidFieldOption = "ERR-013";
        public const string PrimaryFieldMustBeRequired = "ERR-014";
        public const string OnlyOneActivePrimaryFieldAllowed = "ERR-015";
        public const string LastActivePrimaryFieldCannotBeRemoved = "ERR-016";
        public const string ActiveDependencyExists = "ERR-017";
        public const string InvalidStateTransition = "ERR-018";
        public const string PublishedVersionIsImmutable = "ERR-019";
        public const string TemplateNotPublished = "ERR-020";
        public const string OrganizationProvisioningAlreadyRunning = "ERR-021";
        public const string OrganizationProvisioningFailed = "ERR-022";
        public const string IdempotencyKeyReused = "ERR-023";
        public const string OwnerMissing = "ERR-024";
        public const string LastActiveOwnerCannotBeRemoved = "ERR-025";
        public const string OccupantCapacityExceeded = "ERR-026";
        public const string RenterAlreadyHasActiveStay = "ERR-028";
        public const string UnsupportedFileType = "ERR-029";
        public const string FileTooLarge = "ERR-030";
        public const string FilePendingSecurityScan = "ERR-031";
        public const string FileRejectedBySecurityScan = "ERR-032";
        public const string FileQuarantined = "ERR-033";
        public const string RateLimitExceeded = "ERR-040";
        public const string ExternalIdentityConflict = "ERR-041";
        public const string JobRetryLimitExceeded = "ERR-048";
        public const string ServiceUnavailable = "ERR-049";
        public const string UnexpectedError = "ERR-050";
    }

    public static class Parameter
    {
        public const string Object = "object";
        public const string Action = "action";
        public const string Field = "field";
        public const string Dependency = "dependency";
        public const string Service = "service";
    }

    public static class ObjectName
    {
        public const string Field = "field";
        public const string FieldOption = "fieldOption";
        public const string User = "user";
    }
}
