using System.Security.Claims;
using System.Text.Json;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;

namespace AuthService.Api.Features.Audit;

public class AuditLogger(
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditLogger> logger,
    IConfiguration configuration,
    IHostEnvironment environment) : IAuditLogger
{
    private readonly byte[] _hashKey = GetHashKey(configuration, environment);
    private static readonly Action<ILogger, string, string, Guid?, string, Exception?> LogAuditEvent = LoggerMessage.Define<string, string, Guid?, string>(LogLevel.Information, new EventId(1), "AuditEvent {EventType} {Outcome} for actor {ActorUserId}; correlation {CorrelationId}");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public async Task LogAsync<T>(string eventType, string outcome, T eventData, AppDbContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        var actorUserIdStr = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? httpContext?.User?.FindFirstValue("sub");
        Guid? actorUserId = Guid.TryParse(actorUserIdStr, out var parsedId) ? parsedId : null;

        var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        var ipHash = ComputeHash(ip, _hashKey);

        var correlationId = httpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? httpContext?.TraceIdentifier
                            ?? Guid.NewGuid().ToString();

        var metadataJson = eventData != null ? JsonSerializer.Serialize(eventData, JsonOptions) : null;

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            EventType = eventType,
            Outcome = outcome,
            IpHash = ipHash,
            CorrelationId = correlationId,
            Metadata = metadataJson,
            OccurredAtUtc = DateTime.UtcNow
        };

        LogAuditEvent(logger, eventType, outcome, actorUserId, correlationId, null);

        context.AuditEvents.Add(auditEvent);
    }

    private static byte[] GetHashKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredKey = configuration["Audit:HashKey"];
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            return System.Text.Encoding.UTF8.GetBytes(configuredKey);
        }

        if (environment.IsDevelopment())
        {
            return System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        }

        throw new InvalidOperationException("Audit:HashKey must be configured outside Development.");
    }

    private static string ComputeHash(string input, byte[] key)
    {
        var bytes = System.Security.Cryptography.HMACSHA256.HashData(key, System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
