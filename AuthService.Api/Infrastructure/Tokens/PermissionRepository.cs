namespace AuthService.Api.Infrastructure.Tokens;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PermissionRepository : IPermissionRepository
{
    public Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        // TODO: Здесь должен быть запрос к базе данных (EF Core или Dapper).
        // Например: return await _dbContext.UserPermissions.Where(x => x.UserId == userId).Select(x => x.PermissionName).ToListAsync();
        
        // Пока возвращаем тестовые права для успешной компиляции и проверки:
        var dummyPermissions = new List<string>
        {
            "users:read",
            "users:write",
            "reports:generate"
        };

        return Task.FromResult(dummyPermissions);
    }
}
