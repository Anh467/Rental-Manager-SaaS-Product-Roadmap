using RentalManager.Modules.TenantManagement.Application.Fields;
using Xunit;

namespace RentalManager.Modules.TenantManagement.Application.UnitTests;

public sealed class FieldKeyNormalizerTests
{
    [Theory]
    [InlineData("  room_number  ", "room_number")]
    [InlineData("\tRoom\t", "Room")]
    [InlineData("Room", "Room")]
    public void Trim_removes_surrounding_whitespace(string input, string expected)
    {
        Assert.Equal(expected, FieldKeyNormalizer.Trim(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Trim_maps_absent_values_to_empty(string? input)
    {
        Assert.Equal(string.Empty, FieldKeyNormalizer.Trim(input));
    }

    [Theory]
    [InlineData("  room_number ", "ROOM_NUMBER")]
    [InlineData("RoomNumber", "ROOMNUMBER")]
    [InlineData("số_phòng", "SỐ_PHÒNG")]
    public void Normalize_trims_and_upper_cases(string input, string expected)
    {
        Assert.Equal(expected, FieldKeyNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_treats_case_variants_as_the_same_key()
    {
        Assert.Equal(
            FieldKeyNormalizer.Normalize("RoomNumber"),
            FieldKeyNormalizer.Normalize(" roomnumber "));
    }

    [Fact]
    public void Normalize_uses_invariant_casing()
    {
        // Turkish dotless i is the classic case where a culture-sensitive
        // ToUpper would produce a different key on a different server.
        Assert.Equal("ID", FieldKeyNormalizer.Normalize("id"));
    }
}
