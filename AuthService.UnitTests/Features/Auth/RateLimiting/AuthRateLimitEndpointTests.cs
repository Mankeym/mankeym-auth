using AuthService.Api.Common.RateLimiting;
using AuthService.Api.Features.Auth.ForgotPassword;
using AuthService.Api.Features.Auth.Login;
using AuthService.Api.Features.Auth.Refresh;
using AuthService.Api.Features.Auth.Register;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuthService.UnitTests.Features.Auth.RateLimiting;

public sealed class AuthRateLimitEndpointTests
{
    private static readonly AuthRateLimitResult Rejected = new(false, TimeSpan.FromSeconds(42));

    [Fact]
    public async Task Login_WhenLimitIsExceeded_Returns429AndDoesNotCallHandler()
    {
        var handler = new Mock<ILoginHandler>();
        var limiter = RejectedLimiter();
        var endpoint = new LoginEndPoint(handler.Object, limiter.Object)
        {
            ControllerContext = ControllerContext()
        };

        var result = await endpoint.Post(new LoginRequest { Email = "user@example.com", Password = "Password123" });

        AssertRateLimited(result, endpoint);
        handler.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        limiter.Verify(x => x.TryAcquireAsync(AuthRateLimitPolicy.Login, It.IsAny<string>(), "user@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WhenLimitIsExceeded_Returns429AndDoesNotCallHandler()
    {
        var handler = new Mock<IRegisterHandler>();
        var limiter = RejectedLimiter();
        var endpoint = new RegisterEndPoint(handler.Object, limiter.Object)
        {
            ControllerContext = ControllerContext()
        };

        var result = await endpoint.Post(new RegisterRequest { Email = "user@example.com", Password = "Password123" });

        AssertRateLimited(result, endpoint);
        handler.Verify(x => x.CreateUser(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_WhenLimitIsExceeded_Returns429AndDoesNotCallHandler()
    {
        var handler = new Mock<IForgotPasswordHandler>();
        var limiter = RejectedLimiter();
        var endpoint = new ForgotPasswordEndPoint(handler.Object, limiter.Object)
        {
            ControllerContext = ControllerContext()
        };

        var result = await endpoint.Post(new ForgotPasswordRequest("user@example.com"));

        AssertRateLimited(result, endpoint);
        handler.Verify(x => x.CreateForgotPasswordLink(It.IsAny<ForgotPasswordRequest>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_WhenLimitIsExceeded_Returns429AndDoesNotCallHandler()
    {
        var handler = new Mock<IRefreshHandler>();
        var limiter = RejectedLimiter();
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "refreshToken=test-refresh-token";
        var endpoint = new RefreshEdnPoint(handler.Object, limiter.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = await endpoint.Post();

        AssertRateLimited(result, endpoint);
        handler.Verify(x => x.RefreshTokensAsync(It.IsAny<string>()), Times.Never);
    }

    private static Mock<IAuthRateLimiter> RejectedLimiter()
    {
        var limiter = new Mock<IAuthRateLimiter>();
        limiter.Setup(x => x.TryAcquireAsync(
                It.IsAny<AuthRateLimitPolicy>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rejected);
        return limiter;
    }

    private static ControllerContext ControllerContext() =>
        new() { HttpContext = new DefaultHttpContext() };

    private static void AssertRateLimited(IActionResult result, ControllerBase endpoint)
    {
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        endpoint.Response.Headers.RetryAfter.ToString().Should().Be("42");
    }
}
