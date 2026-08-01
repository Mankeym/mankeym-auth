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
        // ДОБАВЛЕНО: Достаем UserManager и IConfiguration
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // === 1. Создаём базовые разрешения ===
            var permissionsToSeed = new List<Permission>
            {
                new() { Code = "profile:read", Description = "View own profile" },
                new() { Code = "profile:update", Description = "Update own profile" },
                new() { Code = "profile:delete", Description = "Delete own profile" },
                new() { Code = "reports:view", Description = "View reports" },
                new() { Code = "reports:create", Description = "Create reports" },
                new() { Code = "users:manage", Description = "Manage users" },
                new() { Code = "audit:view", Description = "View audit logs" }
            };

            foreach (var perm in permissionsToSeed)
            {
                if (!await context.Permissions.AnyAsync(p => p.Code == perm.Code))
                {
                    context.Permissions.Add(perm);
                }
            }
            await context.SaveChangesAsync();

            var allDbPermissions = await context.Permissions.ToListAsync();

            // === 2. Определяем и создаем роли ===
            var rolePermissions = new Dictionary<string, List<string>>
            {
                ["User"] = new List<string> { "profile:read", "profile:update" },
                ["Moderator"] = new List<string> { "profile:read", "profile:update", "reports:view" },
                ["Admin"] = new List<string> { "profile:*", "reports:*", "users:manage", "audit:view" }
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


            // === 3. ДОБАВЛЕНО: Создаем Суперюзера (Админа) ===

            // Читаем данные из .env / переменных окружения
            var adminEmail = configuration["ADMIN_EMAIL"];
            var adminPassword = configuration["ADMIN_PASSWORD"];
            var adminRole = configuration["ADMIN_ROLE"] ?? "Admin";

            // Если email и пароль заданы в конфиге, пытаемся создать
            if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
            {
                // Ищем пользователя по Email
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true, // Сразу подтверждаем email админу
                        CreatedAtUTC = DateTime.UtcNow,
                        UpdatedAtUTC = DateTime.UtcNow
                        // Если в вашем ApplicationUser есть FirstName и LastName, раскомментируйте:
                        // FirstName = configuration["ADMIN_FIRST_NAME"],
                        // LastName = configuration["ADMIN_LAST_NAME"]
                    };

                    var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createAdminResult.Succeeded)
                    {
                        // Выдаем роль Admin
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
                    // Пользователь уже существует. Убедимся, что у него точно есть админская роль.
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
