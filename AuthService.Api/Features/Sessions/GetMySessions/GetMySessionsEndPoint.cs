using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Sessions.GetMySessions;

[Authorize]
[ApiController]
[Route("api/v1/sessions/me")]
public class GetMySessionsEndPoint(IGetMySessionsHandler getMySessionsHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await getMySessionsHandler.GetMySessions(User);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Sessions);
    }
}
