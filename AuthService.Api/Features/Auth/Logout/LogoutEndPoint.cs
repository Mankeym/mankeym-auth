using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.Logout;

[ApiController]
[Route("api/v1/auth/logout")]
public class LogoutEndPoint(ILogoutHandler logoutHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post()
    {
        Request.Cookies.TryGetValue("refreshToken", out var refreshToken);

        var result = await logoutHandler.LogoutAsync(refreshToken);

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });

        if (!result.Success)
        {
            return BadRequest(new { Error = result.Error });
        }

        return Ok(new { message = "Logged out successfully." });
    }
}
