using RentalManager.Modules.TenantManagement.Application.Fields.Dtos;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Core.Exceptions;

namespace RentalManager.Modules.TenantManagement.Application.Fields;

/// <summary>
/// Request-shape validation for field commands. Collects every failure so the
/// client can show them all at once.
/// </summary>
public static class FieldValidator
{
    private const int MaxKeyLength = 256;
    private const int MaxNameLength = 256;
    private const int MaxDescriptionLength = 1028;

    public static void ValidateCreate(CreateFieldRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var failures = new List<ValidationFailure>();

        if (!FieldInvariants.IsSupportedTargetEntityType(request.TargetEntityType))
        {
            failures.Add(new ValidationFailure(
                nameof(CreateFieldRequest.TargetEntityType),
                MessageCode.Error.ValidationFailed));
        }

        ValidateKey(request.Key, failures);
        ValidateName(request.Name, failures);
        ValidateDescription(request.Description, failures);

        if (!FieldInvariants.IsSupportedFieldType(request.FieldTypeId))
        {
            failures.Add(new ValidationFailure(
                nameof(CreateFieldRequest.FieldTypeId),
                MessageCode.Error.FieldTypeMismatch));
        }

        ValidateOptions(request.FieldTypeId, request.Options, failures);

        Throw(failures);
    }

    public static void ValidateUpdate(UpdateFieldRequest request, int storedFieldTypeId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var failures = new List<ValidationFailure>();

        ValidateName(request.Name, failures);
        ValidateDescription(request.Description, failures);
        ValidateRowVersion(request.RowVersion, failures);
        ValidateOptions(storedFieldTypeId, request.Options, failures);

        Throw(failures);
    }

    public static void ValidateDelete(DeleteFieldRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var failures = new List<ValidationFailure>();
        ValidateRowVersion(request.RowVersion, failures);

        Throw(failures);
    }

    /// <summary>
    /// Decodes the concurrency token supplied by the client.
    /// </summary>
    public static byte[] ParseRowVersion(string? rowVersion)
    {
        if (TryParseRowVersion(rowVersion, out byte[]? parsed))
        {
            return parsed;
        }

        throw new ValidationFailedException(
            nameof(UpdateFieldRequest.RowVersion),
            MessageCode.Error.ValidationFailed);
    }

    private static bool TryParseRowVersion(
        string? rowVersion,
        out byte[] parsed)
    {
        parsed = [];

        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[16];

        if (!Convert.TryFromBase64String(rowVersion, buffer, out int written) ||
            written == 0)
        {
            return false;
        }

        parsed = buffer[..written].ToArray();
        return true;
    }

    private static void ValidateKey(string? key, List<ValidationFailure> failures)
    {
        string trimmed = FieldKeyNormalizer.Trim(key);

        if (trimmed.Length == 0 || trimmed.Length > MaxKeyLength)
        {
            failures.Add(new ValidationFailure(
                nameof(CreateFieldRequest.Key),
                MessageCode.Error.ValidationFailed));
        }
    }

    private static void ValidateName(string? name, List<ValidationFailure> failures)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0 || trimmed.Length > MaxNameLength)
        {
            failures.Add(new ValidationFailure(
                nameof(CreateFieldRequest.Name),
                MessageCode.Error.ValidationFailed));
        }
    }

    private static void ValidateDescription(
        string? description,
        List<ValidationFailure> failures)
    {
        if (description is not null && description.Length > MaxDescriptionLength)
        {
            failures.Add(new ValidationFailure(
                nameof(CreateFieldRequest.Description),
                MessageCode.Error.ValidationFailed));
        }
    }

    private static void ValidateRowVersion(
        string? rowVersion,
        List<ValidationFailure> failures)
    {
        if (!TryParseRowVersion(rowVersion, out _))
        {
            failures.Add(new ValidationFailure(
                nameof(UpdateFieldRequest.RowVersion),
                MessageCode.Error.ValidationFailed));
        }
    }

    private static void ValidateOptions(
        int fieldTypeId,
        IReadOnlyList<FieldOptionInput>? options,
        List<ValidationFailure> failures)
    {
        bool requiresOptions = FieldInvariants.RequiresOptions(fieldTypeId);
        int optionCount = options?.Count ?? 0;

        if (!requiresOptions)
        {
            if (optionCount > 0)
            {
                failures.Add(new ValidationFailure(
                    nameof(CreateFieldRequest.Options),
                    MessageCode.Error.InvalidFieldOption));
            }

            return;
        }

        if (optionCount == 0)
        {
            failures.Add(new ValidationFailure(
                nameof(CreateFieldRequest.Options),
                MessageCode.Error.InvalidFieldOption));
            return;
        }

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (FieldOptionInput option in options!)
        {
            string trimmedKey = FieldKeyNormalizer.Trim(option.Key);
            string normalizedKey = FieldKeyNormalizer.Normalize(option.Key);
            string trimmedName = option.Name?.Trim() ?? string.Empty;

            bool isInvalid =
                trimmedKey.Length == 0 ||
                trimmedKey.Length > MaxKeyLength ||
                trimmedName.Length == 0 ||
                trimmedName.Length > MaxNameLength ||
                !seenKeys.Add(normalizedKey);

            if (isInvalid)
            {
                failures.Add(new ValidationFailure(
                    nameof(CreateFieldRequest.Options),
                    MessageCode.Error.InvalidFieldOption));
                return;
            }
        }
    }

    private static void Throw(List<ValidationFailure> failures)
    {
        if (failures.Count > 0)
        {
            throw new ValidationFailedException(failures);
        }
    }
}
