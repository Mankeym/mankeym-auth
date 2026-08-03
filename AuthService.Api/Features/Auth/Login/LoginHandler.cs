using System.Security.Cryptography;
using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Observability;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Security;
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
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class LoginHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext,
    IAuditLogger auditLogger,
    IJwtProvider jwtProvider,
    IHttpContextAccessor httpContextAccessor,
    IPermissionRepository permissionRepository)
    : ILoginHandler
{

    private readonly int _maxAllowedSessions = 5;
    public async Task<LoginResult> Login(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            AuthTelemetry.LoginOutcomes.Add(1, new KeyValuePair<string, object?>("outcome", "invalid_credentials"));
            return InvalidLoginResult();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            AuthTelemetry.LoginOutcomes.Add(1, new KeyValuePair<string, object?>("outcome", "success"));
            return await ProcessSuccessfulLoginAsync(user);
        }

        if (result.IsLockedOut)
        {
            AuthTelemetry.LoginOutcomes.Add(1, new KeyValuePair<string, object?>("outcome", "locked_out"));
            return await ProcessFailedLoginAsync(user, "AccountLocked", "IsLockedOut",
                "Account is locked out due to multiple failed login attempts. Please try again later.");
        }

        if (result.IsNotAllowed)
        {
            AuthTelemetry.LoginOutcomes.Add(1, new KeyValuePair<string, object?>("outcome", "not_allowed"));
            return await ProcessFailedLoginAsync(user, "LoginFailed", "Failed",
                "Login is not allowed. Please confirm your email address.");
        }

        AuthTelemetry.LoginOutcomes.Add(1, new KeyValuePair<string, object?>("outcome", "invalid_credentials"));
        return InvalidLoginResult();
    }
    private async Task<LoginResult> ProcessSuccessfulLoginAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionRepository.GetUserPermissionsAsync(user.Id);
        var securityStamp = await userManager.GetSecurityStampAsync(user);
        var email = user.Email ?? user.UserName ?? throw new InvalidOperationException("User email is missing.");
        string token = await jwtProvider.GenerateAccessToken(user.Id, email, roles, permissions, securityStamp);

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
        await dbContext.SaveChangesAsync();

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
            DeviceName = DeviceNameResolver.Resolve(userAgent),
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
        var userAgent = context?.Request.Headers.UserAgent.ToString() ?? string.Empty;
        var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        return (userAgent, ipAddress);
    }

    private async Task LogAuditAsync(ApplicationUser user, string eventType, string outcome)
    {
        var auditEvent = new UserLoggedInAuditEvent { UserId = user.Id };
        await auditLogger.LogAsync(eventType, outcome, auditEvent, dbContext);
    }

    private static LoginResult InvalidLoginResult()
    {
        return new LoginResult { Success = false, ErrorMessage = "Invalid email or password." };
    }
}
