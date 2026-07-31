namespace AuthService.Api.Infrastructure.Tokens;

using System;
using System.Collections.Generic;

internal interface IJwtProvider
{
    string GenerateAccessToken(Guid userId, IEnumerable<string> roles);
}
