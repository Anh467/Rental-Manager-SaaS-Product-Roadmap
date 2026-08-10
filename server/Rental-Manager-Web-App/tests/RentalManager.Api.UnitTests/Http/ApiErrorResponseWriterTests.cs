using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using RentalManager.Api.Http;
using RentalManager.Api.Middlewares;
using RentalManager.BuildingBlocks.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using Xunit;

namespace RentalManager.Api.UnitTests.Http;

public sealed class ApiErrorResponseWriterTests
{
    private readonly ApiErrorResponseWriter _writer = new();

    [Fact]
    public async Task Writes_status_code_content_type_and_camelCase_body()
    {
        DefaultHttpContext context = CreateContext();

        await _writer.WriteAsync(
            context,
            StatusCodes.Status404NotFound,
            MessageCode.Error.NotFound,
            new Dictionary<string, object?> { [MessageCode.Parameter.Object] = "field" });

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);

        JsonElement root = await ReadBodyAsync(context);
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(MessageCode.Error.NotFound, root.GetProperty("messageKey").GetString());
        Assert.Equal("field", root.GetProperty("parameters").GetProperty("object").GetString());
    }

    [Fact]
    public async Task Omits_empty_parameters_and_field_errors()
    {
        DefaultHttpContext context = CreateContext();

        await _writer.WriteAsync(context, StatusCodes.Status500InternalServerError, MessageCode.Error.UnexpectedError);

        JsonElement root = await ReadBodyAsync(context);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("parameters").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("fieldErrors").ValueKind);
    }

    [Fact]
    public async Task Correlation_id_in_body_matches_the_response_header()
    {
        DefaultHttpContext context = CreateContext();
        context.TraceIdentifier = "correlation-abc-123";

        await _writer.WriteAsync(
            context,
            StatusCodes.Status500InternalServerError,
            MessageCode.Error.UnexpectedError);

        JsonElement root = await ReadBodyAsync(context);
        string headerValue = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        Assert.Equal("correlation-abc-123", headerValue);
        Assert.Equal(headerValue, root.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task Refuses_to_emit_a_deprecated_message_key()
    {
        DefaultHttpContext context = CreateContext();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _writer.WriteAsync(context, StatusCodes.Status400BadRequest, "ERR-034"));

        Assert.Contains("ERR-034", exception.Message);
    }

    [Fact]
    public async Task Refuses_to_emit_an_unknown_message_key()
    {
        DefaultHttpContext context = CreateContext();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _writer.WriteAsync(context, StatusCodes.Status400BadRequest, "ERR-not-a-key"));
    }

    [Fact]
    public async Task Is_a_no_op_once_the_response_has_started()
    {
        DefaultHttpContext context = CreateContext(responseHasStarted: true);

        await _writer.WriteAsync(
            context,
            StatusCodes.Status500InternalServerError,
            MessageCode.Error.UnexpectedError);

        Assert.Equal(0, context.Response.Body.Length);
    }

    private static DefaultHttpContext CreateContext(bool responseHasStarted = false)
    {
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        features.Set<IHttpResponseFeature>(new FakeHttpResponseFeature { HasStarted = responseHasStarted });
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(new MemoryStream()));

        return new DefaultHttpContext(features);
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string raw = await reader.ReadToEndAsync();
        return JsonDocument.Parse(raw).RootElement;
    }

    private sealed class FakeHttpResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted { get; set; }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
