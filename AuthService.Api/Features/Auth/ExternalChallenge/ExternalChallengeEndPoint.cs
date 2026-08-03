using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.ExternalChallenge;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth/external")]
public class ExternalChallengeEndPoint(IExternalChallengeHandler challengeHandler) : ControllerBase
{
    [HttpGet("{provider}")]
    public async Task<IActionResult> Get(
        [FromRoute] string provider,
        [FromQuery] string? returnUrl)
    {
        var request = new ExternalChallengeRequest(provider, returnUrl);

        var result = await challengeHandler.Challenge(request);

        return result;
    }
}
