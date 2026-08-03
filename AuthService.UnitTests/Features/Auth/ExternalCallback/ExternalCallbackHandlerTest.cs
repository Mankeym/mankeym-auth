using System.Security.Claims;
using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.ExternalCallback;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Security;
using AuthService.Api.Infrastructure.Tokens;
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
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IAuditLogger> _auditLoggerMock;
    private readonly ExternalCallbackHandler _handler;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly AppDbContext _dbContext;

    public ExternalCallbackHandlerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            userClaimsPrincipalFactoryMock.Object,
            null!, null!, null!, null!);

        _urlProviderMock = new Mock<IFrontendUrlProvider>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _auditLoggerMock = new Mock<IAuditLogger>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();

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
            _jwtProviderMock.Object,
            _permissionRepositoryMock.Object
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
}
