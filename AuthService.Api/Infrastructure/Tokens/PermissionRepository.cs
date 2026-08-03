using AuthService.Api.Common.Authorization;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Infrastructure.Tokens;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PermissionRepository(AppDbContext dbContext) : IPermissionRepository
{
    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        var roleIds = dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId);

        var permissions = await dbContext.Roles
            .Where(role => roleIds.Contains(role.Id))
            .SelectMany(role => role.Permissions)
            .Select(permission => permission.Code)
            .Distinct()
            .ToListAsync();


        return permissions;
    }
}
