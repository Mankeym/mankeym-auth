using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.Api.Features.Auth.RequestEmailConfirmation;

public record RequestEmailConfirmationRequest(string email);

[ApiController]
[Route("api/v1/auth/request-email-confirmation")]
public class RequestEmailConfirmationEndPoint(IRequestEmailConfirmationHandler handler) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("EmailConfirmationLimit")]
    public async Task<IActionResult> Post([FromBody] RequestEmailConfirmationRequest request)
    {
        var result = await handler.RequestEmailConfirmationAsync(request.email);

        return Ok(result);
    }
}
