namespace AuthService.Api.Infrastructure.Outbox;

/// <summary>
/// Delivers an outbox event to the downstream service. MessageId is an idempotency key:
/// a receiver must persist processed ids and ignore a delivery it has already handled.
/// </summary>
public interface IOutboxTransport
{
    Task DeliverAsync(OutboxDelivery delivery, CancellationToken cancellationToken);
}

public sealed record OutboxDelivery(Guid MessageId, string Type, string Payload);
