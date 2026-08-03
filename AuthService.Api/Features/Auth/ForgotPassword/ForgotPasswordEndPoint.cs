using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.ForgotPassword;

[ApiController]
[Route("api/v1/auth/forgot-password")]
public class ForgotPasswordEndPoint(IForgotPasswordHandler handler): ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(ForgotPasswordRequest request)
    {
        var result = await handler.CreateForgotPasswordLink(request);

        return Accepted(result);
    }
}
