using AuthService.Api.Features.Roles.GetAllRoles;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Roles.GetRolesPermissions;

public interface IGetRolesPermissionsHandler
{
    Task<GetRolesPermissionsResponse> GetRolesPermissionsAsync(GetRolesPermissionsRequest request);
}

public record GetRolesPermissionsResponse(bool Success, List<PermissionDto>? Permissions, string? ErrorMessage);

public record GetRolesPermissionsRequest(string roleName);

public class GetRolesPermissionsHandler(RoleManager<ApplicationRole> roleManager) : IGetRolesPermissionsHandler
{
    public async Task<GetRolesPermissionsResponse> GetRolesPermissionsAsync(GetRolesPermissionsRequest request)
    {
        var normalizedName = roleManager.NormalizeKey(request.roleName);
        var role = await roleManager.Roles
            .Where(x => x.NormalizedName == normalizedName)
            .Select(a =>
                a.Permissions
                    .Select(p => new PermissionDto(p.Code, p.Description))
                    .ToList()
                ).FirstOrDefaultAsync();

        if (role == null)
        {
            return new GetRolesPermissionsResponse(
                false,
                null,
                "Role not found.");
        }

        return new GetRolesPermissionsResponse(true, role, null);
    }
}
