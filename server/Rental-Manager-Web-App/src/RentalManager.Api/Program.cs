using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using RentalManager.Api.Authorization;
using RentalManager.Api.Contracts;
using RentalManager.Api.Middlewares;
using RentalManager.Api.Security;
using RentalManager.BuildingBlocks.Tenancy;
using RentalManager.Modules.TenantManagement.Core.Constants;
using RentalManager.Modules.TenantManagement.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

            return new BadRequestObjectResult(new ApiErrorResponse
            {
                MessageKey = MessageCode.Error.ValidationFailed,
                FieldErrors = fieldErrors,
                CorrelationId = context.HttpContext.TraceIdentifier
            });
        };
    });
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTenancy();
builder.Services.AddTenantManagementInfrastructure(builder.Configuration);

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Issuer) &&
            !string.IsNullOrWhiteSpace(options.Audience) &&
            options.SigningKey.Length >= 32,
        "Jwt configuration requires Issuer, Audience and a SigningKey of at " +
        "least 32 characters.")
    .ValidateOnStart();

builder.Services.AddSingleton<JwtTokenIssuer>();
builder.Services.AddOptions<BootstrapAdminOptions>()
    .Bind(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services.AddHostedService<BootstrapAdminHostedService>();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddPolicy("development", policy => policy
    .WithOrigins("http://localhost:5173", "https://localhost:5173", "http://localhost:4173", "https://localhost:4173")
    .AllowAnyHeader().AllowAnyMethod()));

JwtOptions jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters =
            JwtTokenIssuer.CreateValidationParameters(jwtOptions);
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                return WriteAuthErrorAsync(context.HttpContext, StatusCodes.Status401Unauthorized, MessageCode.Error.AuthenticationRequired);
            },
            OnForbidden = context =>
                WriteAuthErrorAsync(context.HttpContext, StatusCodes.Status403Forbidden, MessageCode.Error.PermissionDenied)
        };
    });

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization();

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

// The exception middleware wraps everything after it so every failure, including
// one raised while binding the organization context, is returned as an envelope.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseHttpsRedirection();
if (app.Environment.IsDevelopment()) app.UseCors("development");
app.UseAuthentication();
app.UseMiddleware<OrganizationContextMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static Task WriteAuthErrorAsync(HttpContext context, int statusCode, string messageKey)
{
    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json; charset=utf-8";
    return context.Response.WriteAsync(JsonSerializer.Serialize(new ApiErrorResponse
    {
        MessageKey = messageKey,
        CorrelationId = context.TraceIdentifier
    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}

/// <summary>
/// Named entry point so the integration test host can boot the real application.
/// </summary>
public partial class Program;
