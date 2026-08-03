using System.Security.Claims;
using AuthService.Api.Common.Authorization;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Для IConfiguration

namespace AuthService.Api.Infrastructure.Seed;

public static class DbInitializer
{
    public static async Task SeedRolesAndPermissionsAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var codePermissions = Permissions.GetAll();

            foreach (var code in codePermissions)
            {
                if (!await context.Permissions.AnyAsync(p => p.Code == code))
                {
                    context.Permissions.Add(new Permission
                    {
                        Code = code,
                        Description = $"Allows to {code.Replace(':', ' ').Replace('.', ' ')}"
                    });
                }
            }
            await context.SaveChangesAsync();

            var allDbPermissions = await context.Permissions.ToListAsync();
            var rolePermissions = new Dictionary<string, List<string>>
            {
                ["User"] = new List<string> { Permissions.ProfileRead, Permissions.ProfileUpdate },
                ["Moderator"] = new List<string> { Permissions.ProfileRead, Permissions.ProfileUpdate, Permissions.AuditView },
                ["Admin"] = new List<string> { Permissions.ProfileAll, Permissions.UsersManage, Permissions.AuditView, Permissions.RolesAll }
            };

            foreach (var (roleName, permissionCodes) in rolePermissions)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null)
                {
                    role = new ApplicationRole { Name = roleName };
                    var createResult = await roleManager.CreateAsync(role);
                    if (!createResult.Succeeded)
                    {
                        logger.LogError("Failed to create role {Role}: {Errors}", roleName, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                        continue;
                    }
                    role = await roleManager.FindByNameAsync(roleName);
                    if (role == null)
                    {
                        logger.LogError("Role {Role} was not found after creation.", roleName);
                        continue;
                    }
                }

                var roleWithPermissions = await context.Roles
                    .Include(r => r.Permissions)
                    .FirstOrDefaultAsync(r => r.Id == role.Id);

                if (roleWithPermissions == null) continue;

                var permsToAdd = new List<Permission>();
                foreach (var code in permissionCodes)
                {
                    if (code.EndsWith("*"))
                    {
                        var prefix = code.TrimEnd('*');
                        permsToAdd.AddRange(allDbPermissions.Where(p => p.Code.StartsWith(prefix)));
                    }
                    else
                    {
                        var perm = allDbPermissions.FirstOrDefault(p => p.Code == code);
                        if (perm != null) permsToAdd.Add(perm);
                    }
                }

                var existingIds = roleWithPermissions.Permissions.Select(p => p.Id).ToHashSet();
                foreach (var perm in permsToAdd.DistinctBy(p => p.Id))
                {
                    if (!existingIds.Contains(perm.Id))
                    {
                        roleWithPermissions.Permissions.Add(perm);
                    }
                }
            }
            await context.SaveChangesAsync();
            logger.LogInformation("Roles and permissions successfully seeded.");

            var adminEmail = configuration["ADMIN_EMAIL"];
            var adminPassword = configuration["ADMIN_PASSWORD"];
            var adminRole = configuration["ADMIN_ROLE"] ?? "Admin";

            if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
            {
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        CreatedAtUTC = DateTime.UtcNow,
                        UpdatedAtUTC = DateTime.UtcNow
                    };

                    var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createAdminResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, adminRole);
                        logger.LogInformation("Superuser '{Email}' created successfully with role '{Role}'.", adminEmail, adminRole);
                    }
                    else
                    {
                        logger.LogError("Failed to create superuser: {Errors}",
                            string.Join(", ", createAdminResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(adminUser, adminRole))
                    {
                        await userManager.AddToRoleAsync(adminUser, adminRole);
                        logger.LogInformation("Role '{Role}' restored for existing superuser '{Email}'.", adminRole, adminEmail);
                    }
                }
            }
            else
            {
                logger.LogWarning("Admin credentials not found in configuration. Superuser creation skipped.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
