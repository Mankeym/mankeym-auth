using AuthService.Api.Common.RateLimiting;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.ForgotPassword;

[ApiController]
[Route("api/v1/auth/forgot-password")]
public class ForgotPasswordEndPoint(IForgotPasswordHandler handler, IAuthRateLimiter rateLimiter) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(ForgotPasswordRequest request)
    {
        var limit = await rateLimiter.TryAcquireAsync(
            AuthRateLimitPolicy.PasswordReset,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            request.Email,
            HttpContext.RequestAborted);

        if (!limit.IsAllowed)
        {
            Response.Headers.RetryAfter = Math.Ceiling(limit.RetryAfter.TotalSeconds).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many password reset attempts." });
        }

        var result = await handler.CreateForgotPasswordLink(request);

        return Accepted(result);
    }
}
