using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.ExternalCallback;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AuthService.Api.Features.Auth.ExternalChallenge;

public record ExternalChallengeRequest(string Provider, string? ReturnUrl);

public interface IExternalChallengeHandler
{
    Task<IActionResult> Challenge(ExternalChallengeRequest request);
}

public class ExternalChallengeHandler(
    SignInManager<ApplicationUser> signInManager,
    LinkGenerator linkGenerator,
    IAuditLogger auditLogger,
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider schemes) : IExternalChallengeHandler
{
    public async Task<IActionResult> Challenge(ExternalChallengeRequest request)
    {
        var (provider, returnUrl) = request;
        if (await schemes.GetSchemeAsync(provider) is null)
        {
            return new ObjectResult(new { error = "External provider is not configured." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }

        var httpContext = httpContextAccessor.HttpContext
                          ?? throw new InvalidOperationException("HttpContext is missing");

        await auditLogger.LogAsync(
            eventType: "ExternalAuth",
            outcome: "Challenge_Initiated",
            eventData: new
            {
                Provider = provider
            },
            context: dbContext);
        await dbContext.SaveChangesAsync();

        var redirectUrl = linkGenerator.GetPathByAction(
            httpContext,
            action: "Callback",
            controller: "ExternalCallbackEndpoint",
            values: new { provider, returnUrl });

        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

        IActionResult result = new ChallengeResult(provider, properties);
        return result;
    }
}
