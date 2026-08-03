using System.Security.Cryptography;
using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Auth.Refresh;

public interface IRefreshHandler
{
    Task<RefreshResult> RefreshTokensAsync(string rawRequestToken);
}

public class RefreshResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
}

public class RefreshHandler(
    AppDbContext dbContext,
    IJwtProvider jwtProvider,
    IAuditLogger auditLogger,
    UserManager<ApplicationUser> userManager) : IRefreshHandler
{
    public async Task<RefreshResult> RefreshTokensAsync(string rawRequestToken)
    {
        var requestTokenHash = TokenSecurityHelper.ComputeSha256Hash(rawRequestToken);

        var existingToken = await dbContext.RefreshTokens
            .Include(t => t.User)
            .Include(t => t.Session)
            .FirstOrDefaultAsync(t => t.TokenHash == requestTokenHash);

        if (existingToken == null)
            return RefreshFailed("Token not found.");

        if (existingToken.Session.RevokedAtUtc != null)
            return RefreshFailed("Session has been terminated.");

        if (existingToken.ExpiresAtUtc < DateTime.UtcNow)
            return RefreshFailed("Token has expired.");

        if (existingToken.UsedAtUtc != null || existingToken.RevokedAtUtc != null)
        {
            if (existingToken.UsedAtUtc != null)
            {
                var timeSinceUsed = DateTime.UtcNow - existingToken.UsedAtUtc.Value;
                if (timeSinceUsed.TotalSeconds <= 15)
                {
                    return RefreshFailed("Parallel refresh request detected.");
                }
            }

            await HandleTokenReuseAsync(existingToken);
            return RefreshFailed("Suspicious activity detected. Session revoked.");
        }

        try
        {
            return await ProcessSuccessfulRefreshAsync(existingToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();

            await HandleTokenReuseAsync(existingToken);

            return RefreshFailed("Parallel refresh request detected (Security Alert).");
        }
    }

    private async Task<RefreshResult> ProcessSuccessfulRefreshAsync(RefreshToken existingToken)
    {
        var utcNow = DateTime.UtcNow;
        existingToken.UsedAtUtc = utcNow;
        existingToken.Session.LastSeenAtUtc = utcNow;

        var rawNewToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var newTokenId = Guid.NewGuid();

        var newRefreshToken = new RefreshToken
        {
            Id = newTokenId,
            SessionId = existingToken.SessionId,
            UserId = existingToken.UserId,
            TokenHash = TokenSecurityHelper.ComputeSha256Hash(rawNewToken),
            FamilyId = existingToken.FamilyId,
            ExpiresAtUtc = utcNow.AddDays(7),
            CreatedAtUtc = utcNow
        };

        existingToken.ReplacedByTokenId = newTokenId;

        dbContext.RefreshTokens.Add(newRefreshToken);
        await dbContext.SaveChangesAsync();

        var roles = await userManager.GetRolesAsync(existingToken.User);
        var newAccessToken = await jwtProvider.GenerateAccessToken(
            existingToken.User.Id,
            existingToken.User.Email!,
            roles);

        return new RefreshResult
        {
            Success = true,
            AccessToken = newAccessToken,
            RefreshToken = rawNewToken
        };
    }

    private async Task HandleTokenReuseAsync(RefreshToken existingToken)
    {
        var now = DateTime.UtcNow;

        await dbContext.UserSessions
            .Where(s => s.Id == existingToken.SessionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.RevokedAtUtc, now)
                .SetProperty(u => u.RevokeReason, "Token reuse detected (Security Alert)"));

        await dbContext.RefreshTokens
            .Where(t => t.FamilyId == existingToken.FamilyId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, now));

        await auditLogger.LogAsync("TokenReuseDetected", "Token reuse detected (Security Alert)",
            new { existingToken.UserId, existingToken.SessionId });
    }

    private static RefreshResult RefreshFailed(string error) =>
        new() { Success = false, Error = error };
}
