using Microsoft.Data.SqlClient;

namespace RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Common;

/// <summary>
/// Translates SQL Server errors into business meaning. Deliberately narrow: a
/// violation is only recognised when both the error number and the offending
/// constraint match, so an unrelated failure never surfaces as a duplicate.
/// </summary>
public static class SqlExceptionClassifier
{
    /// <summary>Duplicate key in a unique index.</summary>
    public const int UniqueIndexViolation = 2601;

    /// <summary>Duplicate key in a unique or primary key constraint.</summary>
    public const int UniqueConstraintViolation = 2627;

    /// <summary>
    /// Decides whether one SQL error is a uniqueness violation of a specific
    /// constraint or index. Kept free of <see cref="SqlException"/> so it is
    /// directly unit testable.
    /// </summary>
    public static bool IsUniqueViolation(
        int errorNumber,
        string? errorMessage,
        string constraintName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(constraintName);

        if (errorNumber is not (UniqueIndexViolation or UniqueConstraintViolation))
        {
            return false;
        }

        return errorMessage is not null &&
               errorMessage.Contains(constraintName, StringComparison.Ordinal);
    }

    /// <summary>
    /// True when any error in the exception is a uniqueness violation of the
    /// given constraint or index.
    /// </summary>
    public static bool IsUniqueViolation(
        SqlException exception,
        string constraintName)
    {
        ArgumentNullException.ThrowIfNull(exception);

        foreach (SqlError error in exception.Errors)
        {
            if (IsUniqueViolation(error.Number, error.Message, constraintName))
            {
                return true;
            }
        }

        return false;
    }
}
