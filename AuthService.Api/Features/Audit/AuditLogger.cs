using System.Security.Claims;
using System.Text.Json;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;

namespace AuthService.Api.Features.Audit;

public class AuditLogger(
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditLogger> logger) : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new() {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public async Task LogAsync<T>(string eventType, string outcome, T eventData, AppDbContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        var actorUserIdStr = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? httpContext?.User?.FindFirstValue("sub");
        Guid? actorUserId = Guid.TryParse(actorUserIdStr, out var parsedId) ? parsedId : null;

        var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        var ipHash = ComputeHash(ip);

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

        logger.LogInformation(
            "AuditEvent {EventType} {Outcome} for actor {ActorUserId}; correlation {CorrelationId}",
            eventType, outcome, actorUserId, correlationId);

        context.AuditEvents.Add(auditEvent);
    }

    private static string ComputeHash(string input)
    {
        var saltedInput = input + "Your_App_Specific_Salt";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(saltedInput));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
