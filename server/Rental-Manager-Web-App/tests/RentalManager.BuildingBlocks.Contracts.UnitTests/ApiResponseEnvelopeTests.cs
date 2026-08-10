using System.Text.Json;
using RentalManager.BuildingBlocks.Contracts;
using Xunit;

namespace RentalManager.BuildingBlocks.Contracts.UnitTests;

public sealed class ApiResponseEnvelopeTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Create_requires_a_non_empty_message_key()
    {
        Assert.Throws<ArgumentException>(() =>
            ApiResponse<string>.Create("data", string.Empty, "correlation-1"));
    }

    [Fact]
    public void Create_requires_a_non_empty_correlation_id()
    {
        Assert.Throws<ArgumentException>(() =>
            ApiResponse<string>.Create("data", "SCS-001", string.Empty));
    }

    [Fact]
    public void Success_envelope_serializes_to_camelCase_with_required_fields()
    {
        ApiResponse<string> response = ApiResponse<string>.Create(
            "payload",
            "SCS-001",
            "correlation-123",
            new Dictionary<string, object?> { ["object"] = "field" });

        string json = JsonSerializer.Serialize(response, SerializerOptions);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("SCS-001", root.GetProperty("messageKey").GetString());
        Assert.Equal("payload", root.GetProperty("data").GetString());
        Assert.Equal("correlation-123", root.GetProperty("correlationId").GetString());
        Assert.Equal("field", root.GetProperty("parameters").GetProperty("object").GetString());
    }

    [Fact]
    public void Success_envelope_correlation_id_is_never_null_or_empty()
    {
        ApiResponse<string> response = ApiResponse<string>.Create("payload", "SCS-001", "correlation-abc");

        Assert.False(string.IsNullOrEmpty(response.CorrelationId));
    }

    [Fact]
    public void Error_envelope_serializes_to_camelCase_with_required_fields()
    {
        var response = new ApiErrorResponse
        {
            MessageKey = "ERR-001",
            CorrelationId = "correlation-456",
            FieldErrors =
            [
                new ApiFieldError("email", "ERR-011")
            ]
        };

        string json = JsonSerializer.Serialize(response, SerializerOptions);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("ERR-001", root.GetProperty("messageKey").GetString());
        Assert.Equal("correlation-456", root.GetProperty("correlationId").GetString());
        Assert.Equal(1, root.GetProperty("fieldErrors").GetArrayLength());
        Assert.Equal("email", root.GetProperty("fieldErrors")[0].GetProperty("fieldKey").GetString());
    }

    [Fact]
    public void Error_envelope_correlation_id_is_never_null_or_empty()
    {
        var response = new ApiErrorResponse
        {
            MessageKey = "ERR-050",
            CorrelationId = "correlation-xyz"
        };

        Assert.False(string.IsNullOrEmpty(response.CorrelationId));
    }

    [Fact]
    public void Page_result_maps_field_by_field()
    {
        var page = new ApiPageResult<int>([1, 2, 3], Page: 1, PageSize: 3, TotalItems: 3, TotalPages: 1);

        Assert.Equal([1, 2, 3], page.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(3, page.PageSize);
        Assert.Equal(3, page.TotalItems);
        Assert.Equal(1, page.TotalPages);
    }

    [Theory]
    [InlineData("field.nested", "field.nested")]
    [InlineData("Field", "field")]
    [InlineData("Options[0].Key", "options[0].key")]
    public void JsonPropertyPathMapper_converts_to_camelCase_path(string input, string expected)
    {
        Assert.Equal(expected, JsonPropertyPathMapper.ToCamelCasePath(input));
    }
}
