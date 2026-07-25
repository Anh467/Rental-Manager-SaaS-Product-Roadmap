using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                    ToFieldName(entry.Key),
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
    });

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// The exception middleware wraps everything after it so every failure, including
// one raised while binding the organization context, is returned as an envelope.
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<OrganizationContextMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string ToFieldName(string modelStateKey)
{
    string name = modelStateKey.Contains('.', StringComparison.Ordinal)
        ? modelStateKey[(modelStateKey.LastIndexOf('.') + 1)..]
        : modelStateKey;

    return name.Length == 0
        ? name
        : char.ToLowerInvariant(name[0]) + name[1..];
}

/// <summary>
/// Named entry point so the integration test host can boot the real application.
/// </summary>
public partial class Program;
