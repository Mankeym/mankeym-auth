using System.Text.Json;

namespace AuthService.Api.Features.Audit;

public class AuditLogger(ILogger<IAuditLogger> logger): IAuditLogger
{
    public Task LogAsync<T>(string eventName, T eventData)
    {
        var json = JsonSerializer.Serialize(eventData);
        logger.LogInformation("=== AUDIT EVENT: {EventName} ===\nData: {Data}", eventName, json);

        return Task.CompletedTask;
    }
}
