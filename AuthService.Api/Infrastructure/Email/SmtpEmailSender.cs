using System.Net.Mail;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Api.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IConfiguration configuration,
    ILogger<SmtpEmailSender> logger) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(user.Id, email, "Confirm your email", $"Confirm your email: {confirmationLink}");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(user.Id, email, "Reset your password", $"Reset your password: {resetLink}");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(user.Id, email, "Reset your password", $"Reset your password code: {resetCode}");

    private async Task SendAsync(Guid userId, string recipient, string subject, string body)
    {
        var host = configuration["Smtp:Host"] ?? "localhost";
        var port = int.TryParse(configuration["Smtp:Port"], out var configuredPort) ? configuredPort : 25;

        using var client = new SmtpClient(host, port);
        using var message = new MailMessage("no-reply@authservice.local", recipient, subject, body);
        await client.SendMailAsync(message);

        logger.LogInformation("Email delivery completed for user {UserId}", userId);
    }
}
