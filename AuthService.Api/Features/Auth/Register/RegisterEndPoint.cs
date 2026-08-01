using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.Register;

[ApiController]
[Route("api/v1/auth/register")]
public class RegisterEndPoint(IRegisterHandler registerHandler): ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] RegisterRequest request)
    {

        var result = await registerHandler.CreateUser(request.Email, request.Password);

        if (!result.Success)
        {
            return BadRequest(new { Errors = result.Errors });
        }

        return Ok(result);
    }
}
