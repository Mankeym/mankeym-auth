using System.Security.Cryptography;
using System.Text;
using AuthService.Api.Common.RateLimiting;
using AuthService.Api.Infrastructure.Observability;
using StackExchange.Redis;

namespace AuthService.Api.Infrastructure.RateLimiting;

public sealed class RedisAuthRateLimiter(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisAuthRateLimiter> logger) : IAuthRateLimiter
{
    private static readonly Action<ILogger, string, Exception?> LogUnavailable = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1), "Redis rate limiting is unavailable for policy {Policy}");
    private const string IncrementFixedWindowScript = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end

        local ttl = redis.call('TTL', KEYS[1])
        if count > tonumber(ARGV[2]) then
            return {0, ttl}
        end

        return {1, ttl}
        """;

    public async Task<AuthRateLimitResult> TryAcquireAsync(
        AuthRateLimitPolicy policy,
        string clientIp,
        string? subject,
        CancellationToken cancellationToken)
    {
        var key = CreateKey(policy, clientIp, subject);

        try
        {
            using var activity = AuthTelemetry.ActivitySource.StartActivity("redis.ratelimit.evaluate");
            activity?.SetTag("db.system", "redis");
            activity?.SetTag("redis.operation", "EVAL");
            activity?.SetTag("ratelimit.policy", policy.Name);
            var result = (RedisResult[]?)await connectionMultiplexer.GetDatabase()
                .ScriptEvaluateAsync(
                    IncrementFixedWindowScript,
                    [key],
                    [(long)policy.Window.TotalSeconds, policy.PermitLimit])
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result is not { Length: 2 })
            {
                throw new RedisException("Unexpected rate-limit script response.");
            }

            var isAllowed = (long)result[0] == 1;
            var retryAfterSeconds = Math.Max(1, (long)result[1]);

            return new AuthRateLimitResult(isAllowed, TimeSpan.FromSeconds(retryAfterSeconds));
        }
        catch (RedisException exception)
        {
            // Redis limits are an abuse-protection layer, not the source of security truth.
            // Keep the API available during a Redis outage and rely on alerting/health checks.
            LogUnavailable(logger, policy.Name, exception);
            return new AuthRateLimitResult(true, TimeSpan.Zero);
        }
    }

    private static RedisKey CreateKey(AuthRateLimitPolicy policy, string clientIp, string? subject)
    {
        var material = $"{clientIp.Trim()}:{subject?.Trim().ToUpperInvariant()}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
        return $"ratelimit:auth:{policy.Name}:{hash}";
    }
}
