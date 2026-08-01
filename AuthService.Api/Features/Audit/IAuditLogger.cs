namespace AuthService.Api.Features.Audit;

public interface IAuditLogger
{
    Task LogAsync<T>(string eventName, T eventData);
}
