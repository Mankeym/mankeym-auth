using AuthService.Api.Infrastructure.Persistence.Entities;

namespace AuthService.Api.Infrastructure.Tokens;

public interface IPermissionRepository
{
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
}
