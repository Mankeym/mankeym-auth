using System.Security.Claims;
using System.Security.Cryptography;
using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Security;
using AuthService.Api.Infrastructure.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

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
    IJwtProvider jwtProvider) : IExternalCallbackHandler
{
    public async Task<IActionResult> HandleCallback(ExternalCallbackRequest request)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {

            await auditLogger.LogAsync("ExternalAuth", "Failed_NoProviderInfo", new { request.Provider });
            var errorPath = frontendUrlProvider.GetValidRedirectUrl(
                string.IsNullOrEmpty(request.ReturnUrl) ? null : new Uri(request.ReturnUrl, UriKind.RelativeOrAbsolute),
                "login?error=oauth_failed");
            return new RedirectResult(errorPath.ToString());
        }

        var emailVerifiedClaim = info.Principal.FindFirstValue("email_verified");
        bool isEmailVerified = emailVerifiedClaim != null &&
                               emailVerifiedClaim.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (!isEmailVerified)
        {
            await auditLogger.LogAsync("ExternalAuth", "Failed_EmailNotVerified", new { info.LoginProvider });
            var errorPath = frontendUrlProvider.GetValidRedirectUrl(
                string.IsNullOrEmpty(request.ReturnUrl) ? null : new Uri(request.ReturnUrl, UriKind.RelativeOrAbsolute),
                "login?error=email_not_verified");
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

            await auditLogger.LogAsync("ExternalAuth", "Success_NewUserCreated", new { info.LoginProvider, Email = email, UserId = user.Id });
        }
        else
        {
            var logins = await userManager.GetLoginsAsync(user);

            if (!logins.Any(l => l.LoginProvider == info.LoginProvider))
            {
                await auditLogger.LogAsync("ExternalAuth", "Blocked_RequireManualLinking", new { info.LoginProvider, Email = email, UserId = user.Id });
                Uri? uri = string.IsNullOrEmpty(request.ReturnUrl) ? null : new Uri(request.ReturnUrl, UriKind.RelativeOrAbsolute);
                var requireLinkingPath = frontendUrlProvider.GetValidRedirectUrl(uri, "login");

                var param = new Dictionary<string, string?>
                {
                    { "error", "require_account_linking" },
                    { "email", email },
                    { "provider", info.LoginProvider }
                };

                var finalRedirectUriLinking = QueryHelpers.AddQueryString(requireLinkingPath.ToString(), param);

                return new RedirectResult(finalRedirectUriLinking);
            }

        }

        var roles = await userManager.GetRolesAsync(user);
        string accessToken = await jwtProvider.GenerateAccessToken(user.Id, user.Email, roles);

        string rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenHash = TokenSecurityHelper.ComputeSha256Hash(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            TokenHash = refreshTokenHash,
            UserId = user.Id,
            SessionId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid().ToString(),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync();

        Uri? returnUri = string.IsNullOrEmpty(request.ReturnUrl) ? null : new Uri(request.ReturnUrl, UriKind.RelativeOrAbsolute);
        var basePath = frontendUrlProvider.GetValidRedirectUrl(returnUri, "oauth-success");

        var queryParams = new Dictionary<string, string?>
        {
            { "accessToken", accessToken },
            { "refreshToken", rawRefreshToken }
        };

        var finalRedirectUri = QueryHelpers.AddQueryString(basePath.ToString(), queryParams);

        return new RedirectResult(finalRedirectUri);
    }
}
