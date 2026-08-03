using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.ExternalUnlink;

[ApiController]
[Route("api/v1/auth/external")]
[Authorize]
public class ExternalUnlinkEndPoint(IExternalUnlinkHandler unlinkHandler) : ControllerBase
{
    [HttpDelete("{provider}")]
    public async Task<IActionResult> Unlink([FromRoute] string provider)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { Error = "Неверный токен авторизации." });
        }

        var request = new ExternalUnlinkRequest(userId, provider);

        var result = await unlinkHandler.UnlinkAsync(request);

        return result;
    }
}
