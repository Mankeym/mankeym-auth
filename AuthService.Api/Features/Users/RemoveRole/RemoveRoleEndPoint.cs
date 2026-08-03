using System.Security.Claims;
using AuthService.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Users.RemoveRole;

[ApiController]
[Authorize(Policy = Permissions.UsersManage)]
[Route("api/v1/users/{id}/roles/{role}")]
public class RemoveRoleEndPoint(IRemoveRoleHandler handler) : ControllerBase
{
    [HttpDelete]
    public async Task<IActionResult> Delete(string id, string role)
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(currentUserIdStr))
        {
            return Unauthorized(new { message = "User context is missing." });
        }

        var request = new RemoveRoleRequest(
            UserId: id,
            RoleName: role,
            currentUserId: currentUserIdStr
        );

        var result = await handler.RemoveRoleAsync(request);

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
