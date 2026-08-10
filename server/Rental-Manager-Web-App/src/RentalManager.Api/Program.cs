using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RentalManager.Api.Http;
using RentalManager.Api.Middlewares;
using RentalManager.BuildingBlocks.Contracts;
using RentalManager.BuildingBlocks.Contracts.Messaging;
using RentalManager.BuildingBlocks.Tenancy;
using RentalManager.Modules.Identity.Infrastructure;
using RentalManager.Modules.Identity.Infrastructure.Authentication;
using RentalManager.Modules.Identity.Infrastructure.Options;
using RentalManager.Modules.TenantManagement.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IApiExceptionMapper, ApiExceptionMapper>();
builder.Services.AddSingleton<IApiErrorResponseWriter, ApiErrorResponseWriter>();

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            ApiFieldError[] fieldErrors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => new ApiFieldError(
                    JsonPropertyPathMapper.ToCamelCasePath(entry.Key),
                    MessageCode.Error.ValidationFailed))
                .ToArray();

            int statusCode = ModelStateStatusResolver.Resolve(context);

            return new ApiErrorActionResult(
                statusCode,
                MessageCode.Error.ValidationFailed,
                fieldErrors);
        };
    });
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddTenancy();
builder.Services.AddTenantManagementInfrastructure(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        IApiErrorResponseWriter writer = context.HttpContext.RequestServices
            .GetRequiredService<IApiErrorResponseWriter>();

        await writer.WriteAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            MessageCode.Error.RateLimitExceeded,
            cancellationToken: cancellationToken);
    };

    options.AddPolicy("auth", httpContext =>
    {
        AuthRateLimitOptions limits = httpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<AuthRateLimitOptions>>()
            .CurrentValue;

        string partitionKey = httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, limits.PermitLimit),
                Window = TimeSpan.FromSeconds(Math.Max(1, limits.WindowSeconds)),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddPolicy("development", policy => policy
    .WithOrigins(
        "http://localhost:5173",
        "https://localhost:5173",
        "http://localhost:4173",
        "https://localhost:4173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.PostConfigure<CookieAuthenticationOptions>(
    IdentityAuthenticationSchemes.ApplicationCookie,
    options =>
    {
        options.Events.OnRedirectToLogin = context =>
            context.HttpContext.RequestServices
                .GetRequiredService<IApiErrorResponseWriter>()
                .WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    MessageCode.Error.AuthenticationRequired);

        options.Events.OnRedirectToAccessDenied = context =>
            context.HttpContext.RequestServices
                .GetRequiredService<IApiErrorResponseWriter>()
                .WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    MessageCode.Error.PermissionDenied);
    });

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Rental Manager API v1");
        options.RoutePrefix = "swagger";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("development");
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<OrganizationContextMiddleware>();
app.UseMiddleware<AntiforgeryValidationMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Named entry point so the integration test host can boot the real application.
/// </summary>
public partial class Program;
