using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Users.GetMe;

[ApiController]
[Route("api/v1/users/me")]
public class GetMeEndPoint(IGetMeHandler getMeHandler) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get()
    {
        var result = await getMeHandler.GetMe(User);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(result.User);
    }
}
