using RentalManager.BuildingBlocks.Tenancy;

namespace RentalManager.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor tenantContextAccessor)
    {
        TenantContext? previousContext = tenantContextAccessor.Current;

        try
        {
            tenantContextAccessor.Current = ResolveFromHeaders(context.Request);
            await _next(context);
        }
        finally
        {
            tenantContextAccessor.Current = previousContext;
        }
    }

    private static TenantContext? ResolveFromHeaders(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(
                TenantHeaderNames.TenantId,
                out var tenantIdHeader) ||
            !Guid.TryParse(tenantIdHeader.ToString(), out Guid tenantId))
        {
            return null;
        }

        request.Headers.TryGetValue(
            TenantHeaderNames.TenantCode,
            out var tenantCodeHeader);

        string? tenantCode = string.IsNullOrWhiteSpace(tenantCodeHeader.ToString())
            ? null
            : tenantCodeHeader.ToString();

        return new TenantContext(tenantId, tenantCode);
    }
}
