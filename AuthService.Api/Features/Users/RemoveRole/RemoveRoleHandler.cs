using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Users.RemoveRole;

public interface IRemoveRoleHandler
{
    Task<RemoveRoleResponse> RemoveRoleAsync(RemoveRoleRequest request);
}
public record RemoveRoleRequest(string UserId, string RoleName, string currentUserId);
public record RemoveRoleResponse(bool Success, string Message);

public class RemoveRoleHandler(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IAuditLogger auditLogger)
    : IRemoveRoleHandler
{
    public async Task<RemoveRoleResponse> RemoveRoleAsync(RemoveRoleRequest request)
    {
        var (userId, roleName, _) = request;

        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return new RemoveRoleResponse(false, "User not found.");
        }

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            return new RemoveRoleResponse(false, "Role does not exist.");
        }

        if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            var adminRole = await roleManager.FindByNameAsync("Admin");
            if (adminRole != null)
            {
                var adminRoleId = adminRole.Id.ToString();
                var adminsCount = await dbContext.UserRoles
                    .CountAsync(ur => ur.RoleId.ToString() == adminRoleId);

                if (adminsCount <= 1 && await userManager.IsInRoleAsync(user, "Admin"))
                {
                    return new RemoveRoleResponse(false, "You cannot remove the last remaining administrator.");
                }
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var result = await userManager.RemoveFromRoleAsync(user, roleName);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new RemoveRoleResponse(false, $"Failed to delete role: {errors}");
        }

        await auditLogger.LogAsync(
            "RoleRemoved",
            "Success",
            new { TargetUserId = user.Id, Role = roleName },
            dbContext);

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new RemoveRoleResponse(true, "Role deleted successfully.");

    }
}
