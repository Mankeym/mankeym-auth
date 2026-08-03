using AuthService.Api.Common.RateLimiting;
using FluentAssertions;

namespace AuthService.UnitTests.Common.RateLimiting;

public sealed class AuthRateLimitPolicyTests
{
    [Theory]
    [InlineData("login", 5, 10)]
    [InlineData("register", 3, 60)]
    [InlineData("password-reset", 3, 60)]
    [InlineData("refresh", 20, 5)]
    public void Policies_HaveExpectedLimits(string name, int permitLimit, int windowMinutes)
    {
        var policy = name switch
        {
            "login" => AuthRateLimitPolicy.Login,
            "register" => AuthRateLimitPolicy.Register,
            "password-reset" => AuthRateLimitPolicy.PasswordReset,
            "refresh" => AuthRateLimitPolicy.Refresh,
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };

        policy.Name.Should().Be(name);
        policy.PermitLimit.Should().Be(permitLimit);
        policy.Window.Should().Be(TimeSpan.FromMinutes(windowMinutes));
    }
}
