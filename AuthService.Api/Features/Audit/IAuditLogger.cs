using AuthService.Api.Infrastructure.Persistence;

namespace AuthService.Api.Features.Audit;

public interface IAuditLogger
{
    Task LogAsync<T>(string eventType, string outcome, T eventData, AppDbContext context);
}
