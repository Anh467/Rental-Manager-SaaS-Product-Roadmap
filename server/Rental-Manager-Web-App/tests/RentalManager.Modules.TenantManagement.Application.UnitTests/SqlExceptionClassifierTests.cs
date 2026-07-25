using RentalManager.Modules.TenantManagement.Infrastructure.Persistence.Common;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

/// <summary>
/// The classifier decides whether a SQL Server failure means "duplicate". These
/// tests exercise the primitive overload, because <c>SqlException</c> cannot be
/// constructed outside the driver.
/// </summary>
public sealed class SqlExceptionClassifierTests
{
    private const string KeyIndex = "UQ_Field_OrganizationTargetKey";
    private const string PrimaryIndex = "UX_Field_ActivePrimary";

    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public void Recognises_uniqueness_errors_naming_the_expected_index(int errorNumber)
    {
        string message =
            $"Cannot insert duplicate key row in object 'org.Field' with unique " +
            $"index '{KeyIndex}'. The duplicate key value is (...).";

        Assert.True(SqlExceptionClassifier.IsUniqueViolation(
            errorNumber,
            message,
            KeyIndex));
    }

    [Fact]
    public void Does_not_map_a_uniqueness_error_from_a_different_index()
    {
        string message =
            $"Cannot insert duplicate key row in object 'org.Field' with unique " +
            $"index '{PrimaryIndex}'.";

        Assert.False(SqlExceptionClassifier.IsUniqueViolation(
            2601,
            message,
            KeyIndex));
    }

    [Theory]
    [InlineData(547)]
    [InlineData(2)]
    [InlineData(1205)]
    [InlineData(50000)]
    public void Does_not_map_unrelated_error_numbers(int errorNumber)
    {
        string message =
            $"Something failed and happens to mention {KeyIndex} in passing.";

        Assert.False(SqlExceptionClassifier.IsUniqueViolation(
            errorNumber,
            message,
            KeyIndex));
    }

    [Fact]
    public void Does_not_map_when_there_is_no_message()
    {
        Assert.False(SqlExceptionClassifier.IsUniqueViolation(2627, null, KeyIndex));
    }

    [Fact]
    public void Index_name_match_is_case_sensitive()
    {
        string message = "duplicate key row ... unique index 'uq_field_organizationtargetkey'.";

        Assert.False(SqlExceptionClassifier.IsUniqueViolation(2601, message, KeyIndex));
    }
}
