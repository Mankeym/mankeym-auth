using System.Text;
using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthService.Api.Features.Auth.ConfirmEmail;

public interface IConfirmEmailHandler
{
    Task<ConfirmEmailResponse> ConfirmEmailAsync(ConfirmEmailRequest request);
}

public record ConfirmEmailResponse(string Message, bool Success);

public class ConfirmEmailHandler(
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger)
    : IConfirmEmailHandler
{
    public async Task<ConfirmEmailResponse> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return new ConfirmEmailResponse("Invalid email confirmation token.", false);
        }

        if (user.EmailConfirmed)
        {
            return new ConfirmEmailResponse("Email is already confirmed.", true);
        }

        string decodedToken;
        try
        {
            var decodedBytes = WebEncoders.Base64UrlDecode(request.Token);
            decodedToken = Encoding.UTF8.GetString(decodedBytes);
        }
        catch (FormatException)
        {
            return new ConfirmEmailResponse("Invalid token format.", false);
        }

        var result = await userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            await auditLogger.LogAsync("EmailConfirmationFailed", errors, new { UserId = request.UserId });
            return new ConfirmEmailResponse("Invalid or expired email confirmation token.", false);
        }

        await auditLogger.LogAsync("EmailConfirmed", "Success", new { UserId = request.UserId });

        return new ConfirmEmailResponse("Email successfully confirmed.", true);
    }
}
