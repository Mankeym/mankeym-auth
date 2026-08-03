using AuthService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Roles.GetAllRoles;

public interface IGetAllRolesHandler
{
    Task<GetAllRolesResponse> GetAllRolesAsync();
}

public record PermissionDto(string Code, string? Description);

public record GetAllRolesDto(string? Name, List<PermissionDto> Permissions);
public record GetAllRolesResponse(bool Success, string Message, List<GetAllRolesDto> Roles);

public class GetAllRolesHandler(AppDbContext dbContext) : IGetAllRolesHandler
{
    public async Task<GetAllRolesResponse> GetAllRolesAsync()
    {
        var roles = await dbContext.Roles
            .Select(a => new GetAllRolesDto(
                a.Name,
                a.Permissions.Select(b => new PermissionDto(b.Code, b.Description)).ToList()))
            .ToListAsync();

        return new GetAllRolesResponse(true, "Roles retrieved successfully", roles);
    }
}
