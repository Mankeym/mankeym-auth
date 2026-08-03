using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Superpower.Model;

namespace AuthService.Api.Features.Users.AssignRole;

public interface IAssignRoleHandler
{
    Task<AssignRoleResponse> AssignRoleAsync(AssignRoleRequest request);
}

public record AssignRoleRequest(string UserId, string RoleName, string currentUserId);
public record AssignRoleResponse(bool Success, string Message);

public class AssignRoleHandler(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IAuditLogger auditLogger)
    : IAssignRoleHandler
{
    public async Task<AssignRoleResponse> AssignRoleAsync(AssignRoleRequest request)
    {
        var (userId, roleName, currentUserId) = request;

        if (userId == currentUserId)
        {
            return new AssignRoleResponse(false, "You cannot assign a role to yourself.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new AssignRoleResponse(false, "User not found.");
        }

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            return new AssignRoleResponse(false, "Role does not exist.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var result = await userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new AssignRoleResponse(false, $"Failed to assign role: {errors}");
        }

        await auditLogger.LogAsync(
            "RoleAssigned",
            "Success",
            new { TargetUserId = user.Id, Role = roleName },
            dbContext);

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new AssignRoleResponse(true, "Role assigned successfully.");
    }
}
