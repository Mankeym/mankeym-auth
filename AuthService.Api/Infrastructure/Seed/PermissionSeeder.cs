using AuthService.Api.Common.Authorization;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Infrastructure.Seed;

public static class PermissionSeeder
{
    public static async Task SeedPermissionsAsync(AppDbContext context)
    {
        // Получаем все коды из нашего кодового каталога
        var codePermissions = Permissions.GetAll().ToList();

        // Получаем коды, которые уже записаны в базе
        var existingCodes = await context.Permissions
            .Select(p => p.Code)
            .ToListAsync();

        // Вычисляем, каких кодов еще нет в БД
        var missingCodes = codePermissions
            .Except(existingCodes)
            .ToList();

        if (missingCodes.Count != 0)
        {
            var newPermissions = missingCodes.Select(code => new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Description = $"Allows to {code.Replace('.', ' ')}"
            });

            await context.Permissions.AddRangeAsync(newPermissions);
            await context.SaveChangesAsync();
        }
    }
}
