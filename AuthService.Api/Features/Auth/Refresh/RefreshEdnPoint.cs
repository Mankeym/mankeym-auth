using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.Refresh;

[ApiController]
[Route("api/v1/auth/refresh")]
public class RefreshEdnPoint(IRefreshHandler refreshHandler): ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { error = "Refresh token cookie is missing." });
        }

        var result = await refreshHandler.RefreshTokensAsync(refreshToken);

        if (!result.Success)
        {
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(new { error = result.Error });
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);

        return Ok(new { accessToken = result.AccessToken });
    }
}
