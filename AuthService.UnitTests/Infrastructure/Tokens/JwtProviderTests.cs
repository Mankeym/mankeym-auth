using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using AuthService.Api.Common.Options;
using AuthService.Api.Infrastructure.Tokens;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AuthService.UnitTests.Infrastructure.Tokens;

public sealed class JwtProviderTests
{
    [Fact]
    public async Task GenerateAccessToken_ContainsIdentityAndPermissionClaims_AndExpectedExpiry()
    {
        using var rsa = RSA.Create(2048);
        var options = Options.Create(new JwtOptions
        {
            PrivateKey = rsa.ExportRSAPrivateKeyPem(),
            PublicKey = rsa.ExportRSAPublicKeyPem(),
            Issuer = "tests",
            Audience = "tests",
            ExpiryMinutes = 30
        });
        var before = DateTime.UtcNow;

        var token = await new JwtProvider(options).GenerateAccessToken(
            Guid.NewGuid(), "user@example.com", ["User"], ["roles.read"], "stamp");

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.Issuer.Should().Be("tests");
        parsed.Audiences.Should().Contain("tests");
        parsed.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "roles.read");
        parsed.ValidTo.Should().BeCloseTo(before.AddMinutes(30), TimeSpan.FromSeconds(2));
    }
}
