using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using AuthService.Api.Features.Auth.ExternalCallback;

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
    IHttpContextAccessor httpContextAccessor) : IExternalChallengeHandler
{
    public async Task<IActionResult> Challenge(ExternalChallengeRequest request)
    {
        var (provider, returnUrl) = request;
        var httpContext = httpContextAccessor.HttpContext
                          ?? throw new InvalidOperationException("HttpContext is missing");

        await auditLogger.LogAsync(
            eventType: "ExternalAuth",
            outcome: "Challenge_Initiated",
            eventData: new
            {
                Provider = provider,
                ReturnUrl = returnUrl,
                UserAgent = httpContext.Request.Headers.UserAgent.ToString()
            });

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
