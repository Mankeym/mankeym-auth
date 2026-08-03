using System.Text.Json;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Api.Infrastructure.Outbox;

public sealed class EmailOutboxTransport(IEmailSender<ApplicationUser> emailSender) : IOutboxTransport
{
    public async Task DeliverAsync(OutboxDelivery delivery, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (delivery.Type)
        {
            case "PasswordResetEmailRequested":
            {
                var payload = JsonSerializer.Deserialize<PasswordResetEmailPayload>(delivery.Payload)
                    ?? throw new InvalidOperationException("Invalid outbox payload.");
                var user = new ApplicationUser { Id = payload.UserId };
                await emailSender.SendPasswordResetLinkAsync(user, payload.Email, payload.ResetLink);
                break;
            }
            case "EmailConfirmationEmailRequested":
            {
                var payload = JsonSerializer.Deserialize<EmailConfirmationEmailPayload>(delivery.Payload)
                    ?? throw new InvalidOperationException("Invalid outbox payload.");
                var user = new ApplicationUser { Id = payload.UserId };
                await emailSender.SendConfirmationLinkAsync(user, payload.Email, payload.ConfirmationLink);
                break;
            }
            default:
                throw new InvalidOperationException("Unsupported outbox message type.");
        }
    }
}

internal sealed record PasswordResetEmailPayload(string Email, Guid UserId, string ResetLink);
internal sealed record EmailConfirmationEmailPayload(
    string Email,
    Guid UserId,
    [property: System.Text.Json.Serialization.JsonPropertyName("ResetLink")] string ConfirmationLink);
