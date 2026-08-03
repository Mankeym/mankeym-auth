using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.ResetPassword;

[ApiController]
[Route("api/v1/auth/reset-password")]
public class ResetPasswordEndPoint(IResetPasswordHandler handler): ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(ResetPasswordRequest request)
    {
        var result = await handler.ResetPassword(request);

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }


        return Ok(result.Message);
    }
}
