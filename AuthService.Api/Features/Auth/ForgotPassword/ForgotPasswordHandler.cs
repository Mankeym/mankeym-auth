using System.Text;
using System.Text.Json;
using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthService.Api.Features.Auth.ForgotPassword;

public interface IForgotPasswordHandler
{
    Task<ForgotPasswordResponse> CreateForgotPasswordLink(ForgotPasswordRequest request);
}

public record ForgotPasswordRequest(string Email);
public record ForgotPasswordResponse(bool Success, string Message);

public class ForgotPasswordHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IFrontendUrlProvider urlProvider,
    IAuditLogger auditLogger)
    : IForgotPasswordHandler
{
    public async Task<ForgotPasswordResponse> CreateForgotPasswordLink(ForgotPasswordRequest request)
    {
        var genericMessage = "If an account with this email exists, a reset link has been sent.";

        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null || !user.EmailConfirmed)
        {
            return new ForgotPasswordResponse(true, genericMessage);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = urlProvider.GetValidRedirectUrl(null, "reset-password");

        var expiresAt = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        var resetLink = $"{baseUrl}?userId={user.Id}&token={encodedToken}&expires={expiresAt}";

        var outboxMessage = new OutboxMessage
        {
            Type = "PasswordResetEmailRequested",
            Payload = JsonSerializer.Serialize(new
            {
                Email = request.Email,
                UserId = user.Id,
                ResetLink = resetLink
            })
        };

        dbContext.OutboxMessages.Add(outboxMessage);

        await auditLogger.LogAsync("ForgotPasswordRequested", "Success", new { UserId = user.Id }, dbContext);

        await dbContext.SaveChangesAsync();

        return new ForgotPasswordResponse(true, genericMessage );
    }
}
