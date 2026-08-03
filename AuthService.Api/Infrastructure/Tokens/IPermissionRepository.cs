namespace AuthService.Api.Infrastructure.Tokens;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPermissionRepository
{
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
}
