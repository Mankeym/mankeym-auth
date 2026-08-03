using AuthService.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Roles.GetRolesPermissions;

[ApiController]
[Route("/api/v1/roles/{role}/permissions")]
[Authorize(Policy = Permissions.RolesRead)]
public class GetRolesPermissionsEndPoint(IGetRolesPermissionsHandler handler): ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromRoute(Name = "role")] string roleName)
    {
        var result = await handler.GetRolesPermissionsAsync(new GetRolesPermissionsRequest(roleName));

        if (!result.Success)
        {
            return NotFound(new { error = result.ErrorMessage });
        }

        return Ok(result.Permissions);
    }
}
