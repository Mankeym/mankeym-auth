using AuthService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Infrastructure.BackgroundJobs;

public class TokenCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TokenCleanupBackgroundService> logger) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Token Cleanup Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while cleaning up expired tokens.");
            }

            // Ждем перед следующим запуском
            await Task.Delay(_checkInterval, stoppingToken);
        }

        logger.LogInformation("Token Cleanup Background Service is stopping.");
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        var thresholdDate = now.AddDays(-7);

        logger.LogInformation("Running token cleanup job at {Time}", now);

        // Используем ExecuteDeleteAsync для эффективного удаления на уровне БД
        var deletedTokensCount = await dbContext.RefreshTokens
            .Where(t => t.ExpiresAtUtc < now || (t.RevokedAtUtc != null && t.RevokedAtUtc < thresholdDate))
            .ExecuteDeleteAsync(cancellationToken);

        // Также можно параллельно подчищать старые пустые/отозванные сессии, у которых не осталось активных токенов
        var deletedSessionsCount = await dbContext.UserSessions
            .Where(s => s.RevokedAtUtc != null && s.RevokedAtUtc < thresholdDate && !s.RefreshTokens.Any(t => t.RevokedAtUtc == null))
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedTokensCount > 0 || deletedSessionsCount > 0)
        {
            logger.LogInformation(
                "Token cleanup completed. Deleted {TokenCount} expired tokens and {SessionCount} old sessions.",
                deletedTokensCount,
                deletedSessionsCount);
        }
    }
}
