using System.Security.Cryptography;
using System.Text;

namespace AuthService.Api.Infrastructure.Tokens;

public static class TokenSecurityHelper
{
    public static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
