using System.Security.Claims;
using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.ExternalCallback;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Security;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuthService.UnitTests.Features.Auth.ExternalCallback;

[TestSubject(typeof(ExternalCallbackHandler))]
public class ExternalCallbackHandlerTests
{
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IFrontendUrlProvider> _urlProviderMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IAuditLogger> _auditLoggerMock;
    private readonly ExternalCallbackHandler _handler;
    private readonly AppDbContext _dbContext;

    public ExternalCallbackHandlerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextAccessorMock.SetupGet(accessor => accessor.HttpContext).Returns(new DefaultHttpContext());
        var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            _httpContextAccessorMock.Object,
            userClaimsPrincipalFactoryMock.Object,
            null!, null!, null!, null!);

        _urlProviderMock = new Mock<IFrontendUrlProvider>();
        _auditLoggerMock = new Mock<IAuditLogger>();

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(dbOptions);

        _handler = new ExternalCallbackHandler(
            _signInManagerMock.Object,
            _urlProviderMock.Object,
            _userManagerMock.Object,
            _auditLoggerMock.Object,
            _dbContext,
            _httpContextAccessorMock.Object
        );
    }

    [Fact]
    public async Task HandleCallback_WhenEmailNotVerified_ReturnsRedirectWithError()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@test.com"),
            new("email_verified", "false")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var loginInfo = new ExternalLoginInfo(principal, "Google", "123", "Google");

        _signInManagerMock
            .Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>()))
            .ReturnsAsync(loginInfo);

        _urlProviderMock
            .Setup(p => p.GetValidRedirectUrl(It.IsAny<Uri?>(), It.IsAny<string>()))
            .Returns(new Uri("http://localhost:3000/login?error=email_not_verified"));

        var request = new ExternalCallbackRequest("Google", null);

        // Act
        var result = await _handler.HandleCallback(request);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("email_not_verified", redirectResult.Url);
    }

    [Fact]
    public async Task HandleCallback_WhenLoginSucceeds_SetsRefreshCookieAndDoesNotPutTokensInRedirect()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com"),
            new("email_verified", "true")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var loginInfo = new ExternalLoginInfo(new ClaimsPrincipal(identity), "Google", "123", "Google");
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "test@example.com" };

        _signInManagerMock
            .Setup(manager => manager.GetExternalLoginInfoAsync(It.IsAny<string?>()))
            .ReturnsAsync(loginInfo);
        _userManagerMock.Setup(manager => manager.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock
            .Setup(manager => manager.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo> { new("Google", "123", "Google") });
        _urlProviderMock
            .Setup(provider => provider.GetValidRedirectUrl(It.IsAny<Uri?>(), "oauth-success"))
            .Returns(new Uri("https://yourapp.com/oauth-success"));
        _auditLoggerMock
            .Setup(logger => logger.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), _dbContext))
            .Returns(Task.CompletedTask);

        var result = await _handler.HandleCallback(new ExternalCallbackRequest("Google", null));

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://yourapp.com/oauth-success", redirect.Url);
        Assert.DoesNotContain("token", redirect.Url, StringComparison.OrdinalIgnoreCase);

        var httpContext = _httpContextAccessorMock.Object.HttpContext!;
        var cookie = Assert.Single(httpContext.Response.Headers.SetCookie);
        Assert.StartsWith("refreshToken=", cookie, StringComparison.Ordinal);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        var session = Assert.Single(_dbContext.UserSessions);
        var refreshToken = Assert.Single(_dbContext.RefreshTokens);
        Assert.Equal(session.Id, refreshToken.SessionId);
    }
}
