namespace AuthService.Api.Common.RateLimiting;

public interface IAuthRateLimiter
{
    Task<AuthRateLimitResult> TryAcquireAsync(
        AuthRateLimitPolicy policy,
        string clientIp,
        string? subject,
        CancellationToken cancellationToken);
}

public sealed record AuthRateLimitResult(bool IsAllowed, TimeSpan RetryAfter);

public sealed record AuthRateLimitPolicy(string Name, int PermitLimit, TimeSpan Window)
{
    public static readonly AuthRateLimitPolicy Login = new("login", 5, TimeSpan.FromMinutes(10));
    public static readonly AuthRateLimitPolicy Register = new("register", 3, TimeSpan.FromHours(1));
    public static readonly AuthRateLimitPolicy PasswordReset = new("password-reset", 3, TimeSpan.FromHours(1));
    public static readonly AuthRateLimitPolicy Refresh = new("refresh", 20, TimeSpan.FromMinutes(5));
    public static readonly AuthRateLimitPolicy OAuthCallback = new("oauth-callback", 20, TimeSpan.FromMinutes(5));
}
