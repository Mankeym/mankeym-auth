namespace AuthService.Api.Infrastructure.Persistence.Entities;

public record AuditEvent
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string IpHash { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? Metadata { get; set; } // Или JsonDocument / string для хранения jsonb
    public DateTime OccurredAtUtc { get; set; }

    public virtual ApplicationUser ActorUser { get; set; } = null!;
}
