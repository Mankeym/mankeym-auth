using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.ConfirmEmail;

public record ConfirmEmailRequest(Guid UserId, string Token);
[ApiController]
[Route("api/v1/auth/confirm-email")]
public class ConfirmEmailEndPoint(IConfirmEmailHandler handler): ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ConfirmEmailRequest request )
    {
        var result = await handler.ConfirmEmailAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
