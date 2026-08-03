using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.RateLimiting;
using AuthService.Api.Common.RateLimiting;
using AuthService.Api.Common.Web;
using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.ConfirmEmail;
using AuthService.Api.Features.Auth.ExternalCallback;
using AuthService.Api.Features.Auth.ExternalChallenge;
using AuthService.Api.Features.Auth.ExternalUnlink;
using AuthService.Api.Features.Auth.ForgotPassword;
using AuthService.Api.Features.Auth.Login;
using AuthService.Api.Features.Auth.Logout;
using AuthService.Api.Features.Auth.Refresh;
using AuthService.Api.Features.Auth.Register;
using AuthService.Api.Features.Auth.RequestEmailConfirmation;
using AuthService.Api.Features.Auth.ResetPassword;
using AuthService.Api.Features.Roles.GetAllRoles;
using AuthService.Api.Features.Roles.GetRolesPermissions;
using AuthService.Api.Features.Sessions.GetMySessions;
using AuthService.Api.Features.Sessions.RevokeSession;
using AuthService.Api.Features.Users.AssignRole;
using AuthService.Api.Features.Users.GetMe;
using AuthService.Api.Features.Users.RemoveRole;
using AuthService.Api.Infrastructure.Authorization;
using AuthService.Api.Infrastructure.BackgroundJobs;
using AuthService.Api.Infrastructure.Email;
using AuthService.Api.Infrastructure.HealthChecks;
using AuthService.Api.Infrastructure.Observability;
using AuthService.Api.Infrastructure.Outbox;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.RateLimiting;
using AuthService.Api.Infrastructure.Security;
using AuthService.Api.Infrastructure.Seed;
using AuthService.Api.Infrastructure.Tokens;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;

[assembly: InternalsVisibleTo("AuthService.IntegrationTests")]
[assembly: InternalsVisibleTo("AuthService.UnitTests")]

Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

builder.Configuration.AddJsonFile(
    "/run/secrets/authservice.user-secrets.json",
    optional: true,
    reloadOnChange: false);

if (builder.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(builder.Configuration["JwtSettings:PrivateKey"])
    && string.IsNullOrWhiteSpace(builder.Configuration["JwtSettings:PublicKey"]))
{
    using var rsa = RSA.Create(2048);
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["JwtSettings:PrivateKey"] = rsa.ExportRSAPrivateKeyPem(),
        ["JwtSettings:PublicKey"] = rsa.ExportRSAPublicKeyPem()
    });
}


builder.Configuration.AddEnvironmentVariables();
var connectionString = builder.Configuration.GetConnectionString("DB_CONNECTION")
                       ?? builder.Configuration["DB_CONNECTION"];

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DB_CONNECTION' not found in configuration.");
}

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
        .AddInterceptors(new DbMetricsInterceptor()));

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
                            ?? builder.Configuration["Redis:Configuration"];

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    throw new InvalidOperationException("Redis connection string is not configured.");
}

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
builder.Services.AddSingleton<IAuthRateLimiter, RedisAuthRateLimiter>();

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: new[] { "ready" })
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("AuthService.Auth")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("AuthService.Auth")
        .AddPrometheusExporter());
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddHttpContextAccessor();
// Validation
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<AssignRoleValidator>();



builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>();
builder.Services.AddScoped<IOutboxTransport, EmailOutboxTransport>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddMemoryCache();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
}));

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddScoped<RemoveRoleValidator>();
builder.Services.AddScoped<IGetRolesPermissionsHandler, GetRolesPermissionsHandler>();
builder.Services.AddScoped<IGetAllRolesHandler, GetAllRolesHandler>();
builder.Services.AddScoped<IRemoveRoleHandler, RemoveRoleHandler>();
builder.Services.AddScoped<IAssignRoleHandler, AssignRoleHandler>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IGetMeHandler, GetMeHandler>();
builder.Services.AddScoped<IGetMySessionsHandler, GetMySessionsHandler>();
builder.Services.AddScoped<IRevokeSessionHandler, RevokeSessionHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, PermissionsClaimsTransformation>();
builder.Services.AddScoped<IRegisterHandler, RegisterHandler>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<ILoginHandler, LoginHandler>();
builder.Services.AddScoped<IRefreshHandler, RefreshHandler>();
builder.Services.AddScoped<IResetPasswordHandler, ResetPasswordHandler>();
builder.Services.AddScoped<IForgotPasswordHandler, ForgotPasswordHandler>();
builder.Services.AddScoped<IConfirmEmailHandler, ConfirmEmailHandler>();
builder.Services.AddScoped<ILogoutHandler, LogoutHandler>();
builder.Services.AddScoped<IExternalUnlinkHandler, ExternalUnlinkHandler>();
builder.Services.AddScoped<IExternalCallbackHandler, ExternalCallbackHandler>();
builder.Services.AddScoped<IExternalChallengeHandler, ExternalChallengeHandler>();
builder.Services.AddScoped<IRequestEmailConfirmationHandler, RequestEmailConfirmationHandler>();
builder.Services.AddHostedService<TokenCleanupBackgroundService>();
builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddScoped<IFrontendUrlProvider, FrontendUrlProvider>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.RequireHeaderSymmetry = true;

    var knownProxies = builder.Configuration
        .GetSection("ForwardedHeaders:KnownProxies")
        .Get<string[]>() ?? [];
    foreach (var proxy in knownProxies)
    {
        if (!IPAddress.TryParse(proxy, out var address))
        {
            throw new InvalidOperationException($"ForwardedHeaders:KnownProxies contains invalid IP address '{proxy}'.");
        }

        options.KnownProxies.Add(address);
    }

    var knownNetworks = builder.Configuration
        .GetSection("ForwardedHeaders:KnownNetworks")
        .Get<string[]>() ?? [];
    foreach (var network in knownNetworks)
    {
        options.KnownNetworks.Add(ParseKnownNetwork(network));
    }
});
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
    };
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred."
        };
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity,
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("EmailConfirmationLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.HttpContext.RequestServices.GetService<IProblemDetailsService>() is { } problemDetailsService)
        {
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails =
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Вы превысили лимит запросов. Пожалуйста, подождите."
                }
            });
        }
    };

});

var app = builder.Build();
app.UseStatusCodePages();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    Console.WriteLine("Scalar is available at http://localhost:5071/scalar");
    using var scope = app.Services.CreateScope();

    if (builder.Configuration.GetValue("Database:ApplyMigrations", true))
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    await DbInitializer.SeedRolesAndPermissionsAsync(scope.ServiceProvider);
}
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthCheckResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("live"),
    ResponseWriter = WriteHealthCheckResponse
});
app.MapPrometheusScrapingEndpoint("/metrics");

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    var response = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration
        })
    };
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync(JsonSerializer.Serialize(response));
}

static Microsoft.AspNetCore.HttpOverrides.IPNetwork ParseKnownNetwork(string value)
{
    var parts = value.Split('/', StringSplitOptions.TrimEntries);
    if (parts.Length != 2
        || !IPAddress.TryParse(parts[0], out var networkAddress)
        || !int.TryParse(parts[1], out var prefixLength))
    {
        throw new InvalidOperationException(
            $"ForwardedHeaders:KnownNetworks contains invalid CIDR '{value}'.");
    }

    try
    {
        return new Microsoft.AspNetCore.HttpOverrides.IPNetwork(networkAddress, prefixLength);
    }
    catch (ArgumentOutOfRangeException exception)
    {
        throw new InvalidOperationException(
            $"ForwardedHeaders:KnownNetworks contains invalid CIDR '{value}'.", exception);
    }
}
app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestContentPolicyMiddleware>();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);

public partial class Program { }
