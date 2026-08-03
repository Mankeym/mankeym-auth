using System.Security.Claims;
using AuthService.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Users.AssignRole;

[ApiController]
[Route("api/v1/users/{id}/roles")]
[Authorize(Policy = Permissions.UsersManage)]
public class AssignRoleEndPoint(IAssignRoleHandler handler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromRoute] string id, [FromBody] string roleName)
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(currentUserIdStr))
        {
            return Unauthorized(new { message = "User context is missing." });
        }

        var request = new AssignRoleRequest(
            UserId: id,
            RoleName: roleName,
            currentUserId: currentUserIdStr
        );

        var result = await handler.AssignRoleAsync(request);

        if (!result.Success)
        {
            if (result.Message.Contains("yourself", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }
}
