using Microsoft.AspNetCore.Identity;
using AuthService.Api.Infrastructure.Persistence.Entities;

namespace AuthService.Api.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        // Здесь будет реальная отправка почты (или логирование ссылки в консоль для разработки)
        Console.WriteLine($"[Email Confirmation] To: {email}, Link: {confirmationLink}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        return Task.CompletedTask;
    }
}
