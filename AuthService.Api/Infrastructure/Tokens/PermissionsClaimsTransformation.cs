using System.Security.Claims;
using AuthService.Api.Common.Authorization;
using AuthService.Api.Infrastructure.Persistence.Entities;
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
        if (principal.Identity is not { IsAuthenticated: true } ||
            principal.HasClaim(c => c.Type == "PermissionsLoaded"))
        {
            return principal;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var parsedUserId))
            return principal;

        var clone = principal.Clone();
        var newIdentity = (ClaimsIdentity)clone.Identity!;

        var cacheKey = $"user_permissions_{userId}";
        if (!cache.TryGetValue(cacheKey, out List<string>? permissions))
        {
            permissions = await permissionRepository.GetUserPermissionsAsync(parsedUserId);
            cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(5));
        }

        if (permissions != null)
        {
            foreach (var permission in permissions)
            {
                newIdentity.AddClaim(new Claim("permission", permission));
            }
        }

        newIdentity.AddClaim(new Claim("PermissionsLoaded", "true"));
        return clone;
    }
}
