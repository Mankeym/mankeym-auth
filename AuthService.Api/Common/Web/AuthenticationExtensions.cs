using System.Security.Cryptography;
using AuthService.Api.Common.Options;

namespace AuthService.Api.Common.Web;

using System;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public static class AuthenticationExtensions
{
    private static readonly Action<ILogger, Exception?> LogJwtAuthenticationFailed = LoggerMessage.Define(LogLevel.Warning, new EventId(1), "JWT authentication failed");
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"Section {JwtOptions.SectionName} is missing from configuration");

        var authentication = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(jwtOptions.PublicKey);
            var publicKey = new RsaSecurityKey(rsa);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                // Используем ПУБЛИЧНЫЙ ключ для проверки подписи
                IssuerSigningKey = publicKey
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();

                    LogJwtAuthenticationFailed(logger, context.Exception);
                    return Task.CompletedTask;
                },
            };
        });

        var googleAuthNSection = configuration.GetSection("Authentication:Google");
        var googleClientId = googleAuthNSection["ClientId"];
        var googleClientSecret = googleAuthNSection["ClientSecret"];

        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authentication.AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = googleClientId;
                googleOptions.ClientSecret = googleClientSecret;

                googleOptions.SaveTokens = false;

                googleOptions.SignInScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ExternalScheme;
            });
        }

        services.AddScoped<IJwtProvider, JwtProvider>();

        return services;
    }
}
