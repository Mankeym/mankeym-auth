using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AuthService.Api.Infrastructure.Observability;

public static class AuthTelemetry
{
    public static readonly ActivitySource ActivitySource = new("AuthService.Auth");
    private static readonly Meter Meter = new("AuthService.Auth");
    public static readonly Counter<long> OutboxDeliveryFailures = Meter.CreateCounter<long>("auth.outbox.delivery.failures");
    public static readonly Counter<long> OutboxDeliveries = Meter.CreateCounter<long>("auth.outbox.deliveries");
    public static readonly Counter<long> LoginOutcomes = Meter.CreateCounter<long>("auth.login.outcomes");
    public static readonly Counter<long> RefreshTokenReuse = Meter.CreateCounter<long>("auth.refresh_token_reuse");
    public static readonly Histogram<double> DbCommandDuration = Meter.CreateHistogram<double>("auth.db.command.duration", "ms");
    private static long _outboxBacklog;
    private static long _outboxLagSeconds;
    public static readonly ObservableGauge<long> OutboxBacklog = Meter.CreateObservableGauge("auth.outbox.backlog", () => _outboxBacklog);

    public static void SetOutboxBacklog(long value) => Interlocked.Exchange(ref _outboxBacklog, value);
    public static readonly ObservableGauge<long> OutboxLag = Meter.CreateObservableGauge("auth.outbox.lag", () => _outboxLagSeconds, "s");
    public static void SetOutboxLag(TimeSpan value) => Interlocked.Exchange(ref _outboxLagSeconds, Math.Max(0, (long)value.TotalSeconds));
}
