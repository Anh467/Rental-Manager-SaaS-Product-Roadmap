using Microsoft.AspNetCore.Antiforgery;

namespace RentalManager.Api.Middlewares;

/// <summary>
/// Validates antiforgery tokens for unsafe HTTP methods before controllers run.
/// </summary>
public sealed class AntiforgeryValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (RequiresValidation(context.Request))
        {
            await antiforgery.ValidateRequestAsync(context);
        }

        await next(context);
    }

    private static bool RequiresValidation(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method) ||
            HttpMethods.IsHead(request.Method) ||
            HttpMethods.IsOptions(request.Method) ||
            HttpMethods.IsTrace(request.Method))
        {
            return false;
        }

        PathString path = request.Path;
        if (path.StartsWithSegments("/health") ||
            path.StartsWithSegments("/swagger") ||
            path.StartsWithSegments("/openapi"))
        {
            return false;
        }

        // CSRF token bootstrap itself must remain reachable anonymously.
        return !path.StartsWithSegments("/api/v1/auth/csrf");
    }
}
