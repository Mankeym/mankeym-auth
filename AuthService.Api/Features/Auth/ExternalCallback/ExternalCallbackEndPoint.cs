using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthService.Api.Common.RateLimiting;

namespace AuthService.Api.Features.Auth.ExternalCallback;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth/external")]
public class ExternalCallbackEndPoint(
    IExternalCallbackHandler callbackHandler,
    IAuthRateLimiter rateLimiter) : ControllerBase
{
    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback(
        [FromRoute] string provider,
        [FromQuery] string? returnUrl,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var limit = await rateLimiter.TryAcquireAsync(
            AuthRateLimitPolicy.OAuthCallback, ipAddress, provider, cancellationToken);
        if (!limit.IsAllowed)
        {
            Response.Headers.RetryAfter = Math.Ceiling(limit.RetryAfter.TotalSeconds).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests);
        }

        var request = new ExternalCallbackRequest(provider, returnUrl);

        var result = await callbackHandler.HandleCallback(request);

        return result;
    }
}
