using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Users.GetMe;

public interface IGetMeHandler
{
    Task<GetMeResult> GetMe(ClaimsPrincipal user);
}

public class UserDto
{
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetMeResult
{
    public bool Success { get; set; }
    public UserDto? User { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
public class GetMeHandler(UserManager<ApplicationUser> userManager) : IGetMeHandler
{
    public async Task<GetMeResult> GetMe(ClaimsPrincipal claimsPrincipal)
    {
        var user = await userManager.GetUserAsync(claimsPrincipal);
        if (user == null)
        {
            var userId = claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                         ?? claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                user = await userManager.FindByIdAsync(userId);
            }
        }
        if (user == null)
        {
            return new GetMeResult
            {
                Success = false,
                ErrorMessage = "Unauthorized"
            };
        }
        var roles = await userManager.GetRolesAsync(user);
        return new GetMeResult
        {
            Success = true,
            User = new UserDto
            {
                Email = user.Email ?? string.Empty,
                Roles = roles,
                CreatedAt = user.CreatedAtUTC,
                UpdatedAt = user.UpdatedAtUTC
            }
        };
    }
}
