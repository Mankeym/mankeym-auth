using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using AuthService.Api.Infrastructure.Persistence;

namespace AuthService.Api.Infrastructure.HealthChecks;

public class PostgresHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public PostgresHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Простой запрос к БД
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("PostgreSQL is ready.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unhealthy", ex);
        }
    }
}
