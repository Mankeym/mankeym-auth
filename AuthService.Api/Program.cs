using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.RateLimiting;
using AuthService.Api.Common.Web;
using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.ConfirmEmail;
using AuthService.Api.Features.Auth.ExternalCallback;
using AuthService.Api.Features.Auth.ExternalChallenge;
using AuthService.Api.Features.Auth.ExternalUnlink;
using AuthService.Api.Features.Auth.ForgotPassword;
using AuthService.Api.Features.Auth.Register;
using AuthService.Api.Infrastructure.HealthChecks;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Seed;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AuthService.Api.Features.Auth.Login;
using AuthService.Api.Features.Auth.Logout;
using AuthService.Api.Features.Auth.Refresh;
using AuthService.Api.Features.Auth.RequestEmailConfirmation;
using AuthService.Api.Features.Auth.ResetPassword;
using AuthService.Api.Features.Sessions.GetMySessions;
using AuthService.Api.Features.Sessions.RevokeSession;
using AuthService.Api.Features.Users.AssignRole;
using AuthService.Api.Features.Users.GetMe;
using AuthService.Api.Infrastructure.Authorization;
using AuthService.Api.Infrastructure.BackgroundJobs;
using AuthService.Api.Infrastructure.Email;
using AuthService.Api.Infrastructure.Outbox;
using AuthService.Api.Infrastructure.Security;
using AuthService.Api.Infrastructure.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

[assembly: InternalsVisibleTo("AuthService.IntegrationTests")]

Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Добавление DbContext
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
    options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: new[] { "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddHttpContextAccessor();
// Validation
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<AssignRoleValidator>();



builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Настройка паролей (по желанию)
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;

        // Настройка блокировки (по желанию)
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Настройка пользователя
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

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

    // Если вы доверяете всем прокси (например, в закрытой сети Docker), очистите лимиты
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
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

        // Получаем сервис ProblemDetails из контейнера
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
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    Console.WriteLine("Scalar is available at http://localhost:5071/scalar");
    using var scope = app.Services.CreateScope();
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
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);

public partial class Program { }
