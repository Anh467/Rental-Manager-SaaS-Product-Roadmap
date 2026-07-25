namespace RentalManager.Modules.TenantManagement.Application.Abstractions.Auditing;

/// <summary>
/// What changed, described in terms of configuration properties only. Never
/// carries a request payload and never carries tenant business data.
/// </summary>
public sealed record AuditLogEntry(
    string EntityType,
    Guid EntityId,
    string Action,
    string? ChangeSummary);

/// <summary>
/// Writes audit entries on the ambient session, so an entry is committed and
/// rolled back together with the change it describes.
/// </summary>
public interface IAuditLogWriter
{
    Task WriteAsync(
        AuditLogEntry entry,
        CancellationToken cancellationToken = default);
}

public static class AuditAction
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string PrimaryChanged = "PrimaryChanged";
}
