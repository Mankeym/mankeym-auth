using System.Security.Cryptography;
using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Auth.Login;

public interface ILoginHandler
{
    Task<LoginResult> Login(string email, string password);
}

public record LoginResult
{
    public bool Success { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string ErrorMessage { get; set; }
}

public class LoginHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext,
    IAuditLogger auditLogger,
    IJwtProvider jwtProvider,
    IHttpContextAccessor httpContextAccessor)
    : ILoginHandler
{

    private readonly int _maxAllowedSessions = 5;
    public async Task<LoginResult> Login(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return InvalidLoginResult();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return await ProcessSuccessfulLoginAsync(user);
        }

        if (result.IsLockedOut)
        {
            return await ProcessFailedLoginAsync(user, "AccountLocked", "IsLockedOut",
                "Account is locked out due to multiple failed login attempts. Please try again later.");
        }

        if (result.IsNotAllowed)
        {
            return await ProcessFailedLoginAsync(user, "LoginFailed", "Failed",
                "Login is not allowed. Please confirm your email address.");
        }

        return InvalidLoginResult();
    }
    private async Task<LoginResult> ProcessSuccessfulLoginAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        string token = await jwtProvider.GenerateAccessToken(user.Id, user.Email!, roles);

        await LogAuditAsync(user, "LoginSucceeded", "Success");

        var activeSessions = await dbContext.UserSessions
            .Where(s => s.UserId == user.Id && s.RevokedAtUtc == null)
            .OrderBy(s => s.LastSeenAtUtc)
            .ToListAsync();

        int sessionsToEvictCount = activeSessions.Count - _maxAllowedSessions + 1;

        if (sessionsToEvictCount > 0)
        {
            var now = DateTime.UtcNow;
            var sessionsToEvict = activeSessions.Take(sessionsToEvictCount);

            foreach (var s in sessionsToEvict)
            {
                s.RevokedAtUtc = now;
                s.RevokeReason = "Exceeded max sessions limit";
                await dbContext.RefreshTokens
                    .Where(t => t.SessionId == s.Id && t.RevokedAtUtc == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, now));
            }
        }

        var (rawRefreshToken, session) = CreateSessionWithToken(user.Id);


        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            AccessToken = token,
            RefreshToken = rawRefreshToken
        };
    }

    private async Task<LoginResult> ProcessFailedLoginAsync(ApplicationUser user, string eventType, string outcome, string errorMessage)
    {
        await LogAuditAsync(user, eventType, outcome);

        return new LoginResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    private (string RawToken, UserSession Session) CreateSessionWithToken(Guid userId)
    {
        var utcNow = DateTime.UtcNow;
        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = TokenSecurityHelper.ComputeSha256Hash(rawRefreshToken),
            UserId = userId,
            FamilyId = Guid.NewGuid().ToString(),
            ExpiresAtUtc = utcNow.AddDays(7),
            CreatedAtUtc = utcNow,
        };

        var (userAgent, ipAddress) = GetClientInfo();

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceName = "Unknown", // Или ParseDeviceName(userAgent)
            UserAgentHash = TokenSecurityHelper.ComputeSha256Hash(userAgent),
            IpHash = TokenSecurityHelper.ComputeSha256Hash(ipAddress),
            CreatedAtUtc = utcNow,
            LastSeenAtUtc = utcNow
        };

        session.RefreshTokens.Add(refreshToken);

        return (rawRefreshToken, session);
    }

    private (string UserAgent, string IpAddress) GetClientInfo()
    {
        var context = httpContextAccessor.HttpContext;
        var userAgent = context?.Request.Headers.UserAgent.ToString() ?? "Unknown";
        var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        return (userAgent, ipAddress);
    }

    private async Task LogAuditAsync(ApplicationUser user, string eventType, string outcome)
    {
        var auditEvent = new UserLoggedInAuditEvent { UserId = user.Id, Email = user.Email };
        await auditLogger.LogAsync(eventType, outcome, auditEvent);
    }

    private static LoginResult InvalidLoginResult()
    {
        return new LoginResult { Success = false, ErrorMessage = "Invalid email or password." };
    }
}
