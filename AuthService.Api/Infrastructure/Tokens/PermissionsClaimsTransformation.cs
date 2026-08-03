using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace AuthService.Api.Infrastructure.Tokens;

public class PermissionsClaimsTransformation(
    IPermissionRepository permissionRepository,
    IMemoryCache cache)
    : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // 1. Если пользователь не аутентифицирован или права уже загружены - пропускаем
        if (principal.Identity is not { IsAuthenticated: true } ||
            principal.HasClaim(c => c.Type == "PermissionsLoaded"))
        {
            return principal;
        }

        // 2. Получаем ID пользователя из легкого JWT
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
            return principal;

        // 3. Создаем копию текущего контекста пользователя, чтобы не менять оригинальный
        var clone = principal.Clone();
        var newIdentity = (ClaimsIdentity)clone.Identity!;

        // 4. Загружаем права (желательно из кэша, чтобы не дергать БД на каждый HTTP-запрос)
        var cacheKey = $"user_permissions_{userId}";
        if (!cache.TryGetValue(cacheKey, out List<string>? permissions))
        {
            // Идем в базу данных (или другой микросервис) только если нет в кэше
            permissions = await permissionRepository.GetUserPermissionsAsync(Guid.Parse(userId));

            // Кэшируем на 5 минут (компромисс между скоростью и актуальностью прав)
            cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(5));
        }

        // 5. Добавляем загруженные пермиссии в текущий контекст памяти
        if (permissions != null)
        {
            foreach (var permission in permissions)
            {
                newIdentity.AddClaim(new Claim("Permission", permission));
            }
        }

        // Ставим флаг, чтобы не зациклить трансформацию
        newIdentity.AddClaim(new Claim("PermissionsLoaded", "true"));

        // Возвращаем обогащенного пользователя (API теперь видит все его права)
        return clone;
    }
}
