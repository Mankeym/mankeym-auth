using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.Login;

public class LoginResultDTO
{
    public string AccessToken { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/auth/login")]
public class LoginEndPoint(ILoginHandler loginHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] LoginRequest request)
    {
        var result = await loginHandler.Login(request.Email, request.Password);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }

        LoginResultDTO loginResult = new LoginResultDTO { AccessToken = result.AccessToken };

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7),
            IsEssential = true
        };

        Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);
        return Ok(loginResult);
    }
}
