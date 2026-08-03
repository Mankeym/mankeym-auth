using System.Text;
using System.Text.Json;
using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthService.Api.Features.Auth.RequestEmailConfirmation;


public interface IRequestEmailConfirmationHandler
{
    Task<RequestEmailConfirmationResult> RequestEmailConfirmationAsync(string email);
}

public record RequestEmailConfirmationResult(string Message, bool Success);

public class RequestEmailConfirmationHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IFrontendUrlProvider urlProvider,
    IAuditLogger auditLogger
) : IRequestEmailConfirmationHandler
{
    public async Task<RequestEmailConfirmationResult> RequestEmailConfirmationAsync(string email)
    {
        var genericMessage = "If an account with this email exists, a confirmation link has been sent.";

        var user = await userManager.FindByEmailAsync(email);

        if (user == null || user.EmailConfirmed)
        {
            return new RequestEmailConfirmationResult(genericMessage, true);
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = urlProvider.GetValidRedirectUrl(null, "confirm-email");

        var expiresAt = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        var confirmationLink = $"{baseUrl}?userId={user.Id}&token={encodedToken}&expires={expiresAt}";

        var outboxMessage = new OutboxMessage
        {
            Type = "EmailConfirmationEmailRequested",
            Payload = JsonSerializer.Serialize(new
            {
                Email = email,
                UserId = user.Id,
                ResetLink = confirmationLink
            })
        };
        dbContext.OutboxMessages.Add(outboxMessage);

        await auditLogger.LogAsync("EmailConfirmationRequested", "Success", new { UserId = user.Id }, dbContext);

        await dbContext.SaveChangesAsync();

        return new RequestEmailConfirmationResult(genericMessage, true);
    }
}
