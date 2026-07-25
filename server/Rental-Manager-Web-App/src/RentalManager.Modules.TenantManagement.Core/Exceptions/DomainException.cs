namespace RentalManager.Modules.TenantManagement.Core.Exceptions;

/// <summary>
/// Base exception for every failure that carries a canonical message code
/// from the shared SCS/ERR catalog instead of localized text.
/// </summary>
public class DomainException : Exception
{
    private static readonly IReadOnlyDictionary<string, object?> NoParameters =
        new Dictionary<string, object?>(0);

    public DomainException(
        string messageKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        Exception? innerException = null)
        : base(messageKey, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageKey);

        MessageKey = messageKey;
        Parameters = parameters ?? NoParameters;
    }

    /// <summary>
    /// Canonical message code such as <c>ERR-002</c>. Never localized text.
    /// </summary>
    public string MessageKey { get; }

    /// <summary>
    /// Placeholder values for the message template, for example
    /// <c>object</c>, <c>field</c> or <c>dependency</c>.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; }
}
