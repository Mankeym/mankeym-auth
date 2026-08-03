using System.Security.Cryptography;
using System.Text;
using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.ConfirmEmail;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Auth.ResetPassword;

public interface IResetPasswordHandler
{
    Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request);
}

public record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);

public record ResetPasswordResponse(bool Success, string Message);

public class ResetPasswordHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IAuditLogger auditLogger)
    : IResetPasswordHandler
{
    public async Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return new ResetPasswordResponse(false, "Invalid password reset token.");
        }

        string decodedToken;
        try
        {
            var decodedBytes = WebEncoders.Base64UrlDecode(request.Token);
            decodedToken = Encoding.UTF8.GetString(decodedBytes);
        }
        catch (FormatException)
        {
            return new ResetPasswordResponse(false, "Invalid token format.");
        }

        var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            await auditLogger.LogAsync("ResetPasswordFailed", errors, new { UserId = request.UserId }, dbContext);
            await dbContext.SaveChangesAsync();
            return new ResetPasswordResponse(false, "Invalid or expired password reset token.");
        }

        var now = DateTime.UtcNow;

        await dbContext.UserSessions
            .Where(s => s.UserId == user.Id && s.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.RevokedAtUtc, now)
                .SetProperty(u => u.RevokeReason, "Password reset"));

        await dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(t => t.SetProperty(u => u.RevokedAtUtc, now));

        await auditLogger.LogAsync("PasswordReset", "Success", new { UserId = request.UserId }, dbContext);
        await dbContext.SaveChangesAsync();

        return new ResetPasswordResponse(true, "Password successfully reset. All active sessions have been revoked.");
    }
}
