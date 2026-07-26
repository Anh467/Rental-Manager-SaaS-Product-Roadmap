namespace RentalManager.Modules.TenantManagement.Core.Constants;

/// <summary>
/// Canonical message codes shared with the client. The client owns the
/// localized text, so the backend must only ever emit these codes.
/// </summary>
public static class MessageCode
{
    public static class Success
    {
        public const string Created = "SCS-001";
        public const string Updated = "SCS-002";
        public const string Deactivated = "SCS-003";
        public const string Retrieved = "SCS-005";
        public const string SignedIn = "SCS-017";
    }

    public static class Error
    {
        public const string ValidationFailed = "ERR-001";
        public const string NotFound = "ERR-002";
        public const string AuthenticationRequired = "ERR-003";
        public const string PermissionDenied = "ERR-004";
        public const string OrganizationContextMissing = "ERR-005";
        public const string AlreadyExists = "ERR-007";
        public const string ImmutableProperty = "ERR-008";
        public const string ConcurrencyConflict = "ERR-010";
        public const string FieldTypeMismatch = "ERR-012";
        public const string InvalidFieldOption = "ERR-013";
        public const string RateLimitExceeded = "ERR-040";
        public const string UnexpectedError = "ERR-050";
    }

    public static class Parameter
    {
        public const string Object = "object";
        public const string Action = "action";
        public const string Field = "field";
        public const string Dependency = "dependency";
    }

    public static class ObjectName
    {
        public const string Field = "field";
        public const string FieldOption = "fieldOption";
    }
}
