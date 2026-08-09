using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.BuildingBlocks.Contracts;

namespace RentalManager.Api.Http;

/// <summary>
/// An <see cref="IActionResult"/> that delegates to <see cref="IApiErrorResponseWriter"/>
/// instead of serializing its own envelope, so MVC error paths (such as
/// invalid model state) go through the same writer as the exception
/// middleware rather than double-serializing the response.
/// </summary>
public sealed class ApiErrorActionResult(
    int statusCode,
    string messageKey,
    IReadOnlyList<ApiFieldError>? fieldErrors = null,
    IReadOnlyDictionary<string, object?>? parameters = null) : IActionResult
{
    public Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IApiErrorResponseWriter writer = context.HttpContext.RequestServices
            .GetRequiredService<IApiErrorResponseWriter>();

        return writer.WriteAsync(
            context.HttpContext,
            statusCode,
            messageKey,
            parameters,
            fieldErrors,
            context.HttpContext.RequestAborted);
    }
}
