using AuthService.Api.Infrastructure.Persistence.Entities;

namespace AuthService.Api.Infrastructure.Tokens;

using System;
using System.Collections.Generic;

public interface IJwtProvider
{
    Task<string> GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles,IEnumerable<string> permissions, string securityStamp);
}
