using System.Security.Claims;
using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Sessions.RevokeSession;

public interface IRevokeSessionHandler
{
    Task<RevokeSessionResult> RevokeSessionAsync(Guid sessionId, ClaimsPrincipal claimsPrincipal);
}

public class RevokeSessionResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}

public class RevokeSessionHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IAuditLogger auditLogger
    ): IRevokeSessionHandler
{
    public async Task<RevokeSessionResult> RevokeSessionAsync(Guid sessionId, ClaimsPrincipal claimsPrincipal)
    {
        var userIdString = userManager.GetUserId(claimsPrincipal);

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return new RevokeSessionResult
            {
                Success = false,
                ErrorMessage = "User not found or unauthorized."
            };
        }

        var currentSession = await dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (currentSession == null)
        {
            return new RevokeSessionResult { Success = false, ErrorMessage = "Session not found." };
        }

        if (currentSession.RevokedAtUtc != null)
        {
            return new RevokeSessionResult { Success = true };
        }

        var now = DateTime.UtcNow;

        currentSession.RevokedAtUtc = now;
        currentSession.RevokeReason = "Revoked by user";

        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.SessionId == sessionId && t.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
        }

        await dbContext.SaveChangesAsync();

        await auditLogger.LogAsync("SessionRevoked", "Success", new { UserId = userId, SessionId = sessionId });

        return new RevokeSessionResult { Success = true };
    }
}
