using AuthService.Api.Common.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.Register;

[ApiController]
[Route("api/v1/auth/register")]
public class RegisterEndPoint(IRegisterHandler registerHandler, IAuthRateLimiter rateLimiter) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] RegisterRequest request)
    {
        var limit = await rateLimiter.TryAcquireAsync(
            AuthRateLimitPolicy.Register,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            request.Email,
            HttpContext.RequestAborted);

        if (!limit.IsAllowed)
        {
            Response.Headers.RetryAfter = Math.Ceiling(limit.RetryAfter.TotalSeconds).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many registration attempts." });
        }

        var result = await registerHandler.CreateUser(request.Email, request.Password);

        if (!result.Success)
        {
            return BadRequest(new { Errors = result.Errors });
        }

        return Ok(result);
    }
}
