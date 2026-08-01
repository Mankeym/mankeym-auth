using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.Login;


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

        return Ok(result);
    }
}
