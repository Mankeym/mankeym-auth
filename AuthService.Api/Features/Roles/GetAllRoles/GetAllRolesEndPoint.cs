using AuthService.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Roles.GetAllRoles;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Policy = Permissions.RolesRead)]
public class GetAllRolesEndPoint(IGetAllRolesHandler handler): ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await handler.GetAllRolesAsync();

        return Ok(result.Roles);
    }
}
