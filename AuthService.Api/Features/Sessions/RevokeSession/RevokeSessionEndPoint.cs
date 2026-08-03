using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Sessions.RevokeSession;

[ApiController]
[Route("api/v1/sessions/{sessionId}")]
public class RevokeSessionEndPoint(IRevokeSessionHandler revokeSessionHandler): ControllerBase
{
    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> RevokeSession(Guid sessionId)
    {
        var result = await revokeSessionHandler.RevokeSessionAsync(sessionId, User);

        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }

        return Ok(new { message = "Session revoked successfully." });
    }
}
