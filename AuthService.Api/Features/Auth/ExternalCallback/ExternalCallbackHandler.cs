using System.Security.Claims;
using System.Security.Cryptography;
using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Security;
using AuthService.Api.Infrastructure.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.ExternalCallback;

public record ExternalCallbackRequest(string Provider, string? ReturnUrl);

public interface IExternalCallbackHandler
{
    Task<IActionResult> HandleCallback(ExternalCallbackRequest request);
}

public class ExternalCallbackHandler(
    SignInManager<ApplicationUser> signInManager,
    IFrontendUrlProvider frontendUrlProvider,
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger,
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor) : IExternalCallbackHandler
{
    public async Task<IActionResult> HandleCallback(ExternalCallbackRequest request)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {

            await auditLogger.LogAsync("ExternalAuth", "Failed_NoProviderInfo", new { request.Provider }, dbContext);
            var errorPath = frontendUrlProvider.GetValidRedirectUrl(
                string.IsNullOrEmpty(request.ReturnUrl) ? null : new Uri(request.ReturnUrl, UriKind.RelativeOrAbsolute),
                "login?error=oauth_failed");

            await dbContext.SaveChangesAsync();
            return new RedirectResult(errorPath.ToString());
        }

        var emailVerifiedClaim = info.Principal.FindFirstValue("email_verified");
        bool isEmailVerified = emailVerifiedClaim != null &&
                               emailVerifiedClaim.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (!isEmailVerified)
        {
            await auditLogger.LogAsync("ExternalAuth", "Failed_EmailNotVerified", new { info.LoginProvider }, dbContext);
            var errorPath = frontendUrlProvider.GetValidRedirectUrl(
                string.IsNullOrEmpty(request.ReturnUrl) ? null : new Uri(request.ReturnUrl, UriKind.RelativeOrAbsolute),
                "login?error=email_not_verified");

            await dbContext.SaveChangesAsync();
            return new RedirectResult(errorPath.ToString());
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email)
                    ?? throw new InvalidOperationException("External provider didn't return an email.");

        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            await userManager.CreateAsync(user);
            await userManager.AddLoginAsync(user, info);

            await userManager.AddToRoleAsync(user, "User");

            await auditLogger.LogAsync(
                "ExternalAuth",
                "Success_NewUserCreated",
                new { info.LoginProvider, UserId = user.Id }, dbContext);

            await dbContext.SaveChangesAsync();
        }
        else
        {
            var logins = await userManager.GetLoginsAsync(user);

            if (!logins.Any(l => l.LoginProvider == info.LoginProvider))
            {
                await auditLogger.LogAsync(
                    "ExternalAuth",
                    "Blocked_RequireManualLinking",
                    new { info.LoginProvider, UserId = user.Id },
                    dbContext);
                Uri? uri = string.IsNullOrEmpty(request.ReturnUrl) ? null : new Uri(request.ReturnUrl, UriKind.RelativeOrAbsolute);
                var requireLinkingPath = frontendUrlProvider.GetValidRedirectUrl(uri, "login");

                var param = new Dictionary<string, string?>
                {
                    { "error", "require_account_linking" },
                    { "provider", info.LoginProvider }
                };

                var finalRedirectUriLinking = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
                    requireLinkingPath.ToString(),
                    param);
                await dbContext.SaveChangesAsync();
                return new RedirectResult(finalRedirectUriLinking);
            }

        }

        var httpContext = httpContextAccessor.HttpContext
                          ?? throw new InvalidOperationException("OAuth callback requires an HTTP context.");
        var utcNow = DateTime.UtcNow;
        string rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DeviceName = DeviceNameResolver.Resolve(httpContext.Request.Headers.UserAgent.ToString()),
            UserAgentHash = TokenSecurityHelper.ComputeSha256Hash(httpContext.Request.Headers.UserAgent.ToString()),
            IpHash = TokenSecurityHelper.ComputeSha256Hash(httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty),
            CreatedAtUtc = utcNow,
            LastSeenAtUtc = utcNow
        };

        session.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = TokenSecurityHelper.ComputeSha256Hash(rawRefreshToken),
            UserId = user.Id,
            FamilyId = Guid.NewGuid().ToString(),
            ExpiresAtUtc = utcNow.AddDays(7),
            CreatedAtUtc = utcNow
        });
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();

        httpContext.Response.Cookies.Append("refreshToken", rawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = utcNow.AddDays(7),
            IsEssential = true
        });

        Uri? returnUri = string.IsNullOrEmpty(request.ReturnUrl) ? null : new Uri(request.ReturnUrl, UriKind.RelativeOrAbsolute);
        var basePath = frontendUrlProvider.GetValidRedirectUrl(returnUri, "oauth-success");

        return new RedirectResult(basePath.ToString());
    }
}
