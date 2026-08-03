using AuthService.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Infrastructure.BackgroundJobs;

public class TokenCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TokenCleanupBackgroundService> logger) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);
    private static readonly Action<ILogger, Exception?> LogStarting = LoggerMessage.Define(LogLevel.Information, new EventId(1), "Token Cleanup Background Service is starting.");
    private static readonly Action<ILogger, Exception?> LogFailed = LoggerMessage.Define(LogLevel.Error, new EventId(2), "An error occurred while cleaning up expired tokens.");
    private static readonly Action<ILogger, Exception?> LogStopping = LoggerMessage.Define(LogLevel.Information, new EventId(3), "Token Cleanup Background Service is stopping.");
    private static readonly Action<ILogger, DateTime, Exception?> LogRunning = LoggerMessage.Define<DateTime>(LogLevel.Information, new EventId(4), "Running token cleanup job at {Time}");
    private static readonly Action<ILogger, int, int, Exception?> LogCompleted = LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(5), "Token cleanup completed. Deleted {TokenCount} expired tokens and {SessionCount} old sessions.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarting(logger, null);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                LogFailed(logger, ex);
            }

            // Ждем перед следующим запуском
            await Task.Delay(_checkInterval, stoppingToken);
        }

        LogStopping(logger, null);
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        var thresholdDate = now.AddDays(-7);

        LogRunning(logger, now, null);

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
            LogCompleted(logger, deletedTokensCount, deletedSessionsCount, null);
        }
    }
}
