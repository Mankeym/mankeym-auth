using AuthService.Api.Infrastructure.Persistence.Entities;

namespace AuthService.Api.Infrastructure.Tokens;

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography; // Добавлено для работы с RSA
using Common.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

public class JwtProvider : IJwtProvider
{
    private readonly JwtOptions _options;
    private readonly RsaSecurityKey _key;

    public JwtProvider(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        // Создаем экземпляр RSA и импортируем приватный ключ
        var rsa = RSA.Create();

        // Предполагается, что в JwtOptions теперь есть поле PrivateKey с ключом в формате PEM
        rsa.ImportFromPem(_options.PrivateKey);

        _key = new RsaSecurityKey(rsa);
    }

    public Task<string> GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.ExpiryMinutes);
        var claims = new List<Claim>
        {
            // sub: Subject (Идентификатор пользователя)
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),

            // email: Почта пользователя
            new Claim(JwtRegisteredClaimNames.Email, email),

            // jti: JWT ID (Защита от replay-атак)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            // iat: Issued At (Время создания токена в формате Unix Time)
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // ВАЖНО: Используем асимметричный алгоритм RsaSha256
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials
        );

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return Task.FromResult(tokenValue);
    }
}
