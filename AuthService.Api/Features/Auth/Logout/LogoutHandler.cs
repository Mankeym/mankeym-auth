using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Tokens;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Auth.Logout;


public interface ILogoutHandler
{
    public Task<LogoutResult> LogoutAsync(string? rawRequestToken);
}

public record LogoutResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
}

public class LogoutHandler(AppDbContext dbContext, IAuditLogger auditLogger) : ILogoutHandler
{
    public async Task<LogoutResult> LogoutAsync(string? rawRequestToken)
    {
        if (string.IsNullOrEmpty(rawRequestToken))
        {
            return new LogoutResult { Success = true };
        }

        var requestTokenHash = TokenSecurityHelper.ComputeSha256Hash(rawRequestToken);

        var token = await dbContext.RefreshTokens
            .Include(p => p.Session)
            .FirstOrDefaultAsync(t => t.TokenHash == requestTokenHash);

        if (token == null)
        {
            return new LogoutResult { Success = true };
        }

        var now = DateTime.UtcNow;

        token.Session.RevokedAtUtc = now;
        token.Session.RevokeReason = "User logout";

        var familyTokens = await dbContext.RefreshTokens
            .Where(t => t.FamilyId == token.FamilyId && t.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var t in familyTokens)
        {
            t.RevokedAtUtc = now;
        }

        await auditLogger.LogAsync("UserLogout", "Success", new { token.UserId, token.SessionId }, dbContext);
        await dbContext.SaveChangesAsync();

        return new LogoutResult { Success = true };
    }
}
