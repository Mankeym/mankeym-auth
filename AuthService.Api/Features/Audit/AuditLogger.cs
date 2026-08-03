using System.Security.Claims;
using System.Text.Json;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Audit;

public class AuditLogger(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditLogger> logger) : IAuditLogger
{
    // Настройки сериализации (чтобы избежать ошибок циклических ссылок)
    private static readonly JsonSerializerOptions JsonOptions = new() {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public async Task LogAsync<T>(string eventType, string outcome, T? eventData, CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        var actorUserIdStr = httpContext?.User?.FindFirstValue(ClaimTypes.Email);
        Guid? actorUserId = Guid.TryParse(actorUserIdStr, out var parsedId) ? parsedId : null;

        var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        var ipHash = ComputeHash(ip);

        var correlationId = httpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? httpContext?.TraceIdentifier
                            ?? Guid.NewGuid().ToString();

        // Сериализуем с настройками, чтобы не упасть на циклических ссылках
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

        // Логируем до похода в БД (если БД упадет, в файле/консоли останется след)
        logger.LogInformation(
            "AuditEvent [{EventType}] Outcome: {Outcome} | User: {ActorUserId} | CorrelationId: {CorrelationId} | Metadata: {@EventData}",
            eventType, outcome, actorUserId, correlationId, eventData);

        // Сохраняем в независимом контексте, не затрагивая транзакции бизнес-логики
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.AuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string ComputeHash(string input)
    {
        var saltedInput = input + "Your_App_Specific_Salt";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(saltedInput));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
