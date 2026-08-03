using System.Text.Json;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Infrastructure.Outbox;

public class OutboxProcessor(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender<ApplicationUser>>();

                var messages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAtUtc == null && m.Attempts < MaxAttempts)
                    .OrderBy(m => m.OccurredAtUtc) // Используем твое имя свойства OccurredAtUtc
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    message.Attempts++;

                    try
                    {
                        // Используем switch-выражение или паттерн-матчинг по типу сообщения
                        await (message.Type switch
                        {
                            "PasswordResetEmailRequested" => ProcessPasswordResetAsync(message, dbContext, emailSender, stoppingToken),
                            "EmailConfirmationEmailRequested" => ProcessEmailConfirmationAsync(message, dbContext, emailSender, stoppingToken),
                            _ => throw new InvalidOperationException($"Unknown outbox message type: {message.Type}")
                        });

                        message.ProcessedAtUtc = DateTime.UtcNow;
                        message.Error = null;
                    }
                    catch (Exception ex)
                    {
                        message.Error = ex.Message;
                    }
                }

                if (messages.Count > 0)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception)
            {
                // Защита от падения фонового сервиса при временных проблемах с базой данных
            }

            // Ждем перед следующей итерацией (5 секунд)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
    private static async Task ProcessPasswordResetAsync(
        OutboxMessage message,
        AppDbContext dbContext,
        IEmailSender<ApplicationUser> emailSender,
        CancellationToken stoppingToken)
    {
        var data = JsonSerializer.Deserialize<PasswordResetEmailPayload>(message.Payload);
        if (data == null) return;

        var user = await dbContext.Users.FindAsync(new object[] { data.UserId }, stoppingToken);
        if (user != null)
        {
            await emailSender.SendPasswordResetLinkAsync(user, data.Email, data.ResetLink);
        }
    }

    private static async Task ProcessEmailConfirmationAsync(
        OutboxMessage message,
        AppDbContext dbContext,
        IEmailSender<ApplicationUser> emailSender,
        CancellationToken stoppingToken)
    {
        var data = JsonSerializer.Deserialize<EmailConfirmationEmailPayload>(message.Payload);
        if (data == null) return;

        var user = await dbContext.Users.FindAsync(new object[] { data.UserId }, stoppingToken);
        if (user != null)
        {
            await emailSender.SendConfirmationLinkAsync(user, data.Email, data.ConfirmationLink);
        }
    }
}

internal record PasswordResetEmailPayload(string Email, Guid UserId, string ResetLink);
internal record EmailConfirmationEmailPayload(string Email, Guid UserId, string ConfirmationLink);
