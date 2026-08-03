using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Infrastructure.Outbox;

public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    internal const int MaxAttempts = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing tick failed.");
            }
        }
    }

    internal async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var transport = scope.ServiceProvider.GetRequiredService<IOutboxTransport>();
        var now = DateTime.UtcNow;

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null
                        && m.Attempts < MaxAttempts
                        && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now))
            .OrderBy(m => m.NextAttemptAtUtc)
            .ThenBy(m => m.OccurredAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        AuthTelemetry.SetOutboxBacklog(messages.Count);
        var oldestPending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .MinAsync(m => (DateTime?)m.OccurredAtUtc, cancellationToken);
        AuthTelemetry.SetOutboxLag(oldestPending is null ? TimeSpan.Zero : DateTime.UtcNow - oldestPending.Value);

        foreach (var message in messages)
        {
            message.Attempts++;

            try
            {
                using var activity = AuthTelemetry.ActivitySource.StartActivity("outbox.deliver");
                activity?.SetTag("outbox.message_id", message.Id);
                activity?.SetTag("outbox.type", message.Type);
                await transport.DeliverAsync(
                    new OutboxDelivery(message.Id, message.Type, message.Payload),
                    cancellationToken);
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.NextAttemptAtUtc = null;
                message.Error = null;
                AuthTelemetry.OutboxDeliveries.Add(1, new KeyValuePair<string, object?>("outbox.type", message.Type));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.Error = "Delivery failed.";
                message.NextAttemptAtUtc = message.Attempts < MaxAttempts
                    ? DateTime.UtcNow.Add(GetRetryDelay(message.Attempts))
                    : null;
                logger.LogWarning(ex, "Outbox message {MessageId} delivery attempt {Attempt} failed.", message.Id, message.Attempts);
                AuthTelemetry.OutboxDeliveryFailures.Add(1, new KeyValuePair<string, object?>("outbox.type", message.Type));
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    internal static TimeSpan GetRetryDelay(int attempts) => attempts switch
    {
        1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(5),
        _ => TimeSpan.FromMinutes(15)
    };
}
