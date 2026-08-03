using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.ExternalCallback;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth/external")]
public class ExternalCallbackEndPoint(IExternalCallbackHandler callbackHandler) : ControllerBase
{
    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback(
        [FromRoute] string provider,
        [FromQuery] string? returnUrl)
    {
        var request = new ExternalCallbackRequest(provider, returnUrl);

        var result = await callbackHandler.HandleCallback(request);

        return result;
    }
}
