using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.ExternalChallenge;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Tokens;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuthService.UnitTests.Features.Auth.ExternalChallenge;

[TestSubject(typeof(ExternalChallengeHandler))]
public class ExternalChallengeHandlerTests
{
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<LinkGenerator> _linkGeneratorMock;
    private readonly Mock<IHttpContextAccessor> _contextAccessorMock;
    private readonly Mock<IAuditLogger> _auditLoggerMock;
    private readonly AppDbContext _dbContext;
    private readonly ExternalChallengeHandler _handler;

    public ExternalChallengeHandlerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _contextAccessorMock = new Mock<IHttpContextAccessor>();
        var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            userManagerMock.Object,
            _contextAccessorMock.Object,
            userClaimsPrincipalFactoryMock.Object,
            null!, null!, null!, null!);

        _linkGeneratorMock = new Mock<LinkGenerator>();
        _auditLoggerMock = new Mock<IAuditLogger>();
        _dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "Test-Agent";
        _contextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _handler = new ExternalChallengeHandler(
            _signInManagerMock.Object,
            _linkGeneratorMock.Object,
            _auditLoggerMock.Object,
            _dbContext,
            _contextAccessorMock.Object
        );
    }

    [Fact]
    public async Task Challenge_WhenCalled_LogsAuditAndReturnsChallengeResult()
    {
        // Arrange
        string provider = "Google";
        string returnUrl = "/dashboard";

        _signInManagerMock
            .Setup(s => s.ConfigureExternalAuthenticationProperties(provider, It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(new AuthenticationProperties());

        var request = new ExternalChallengeRequest(provider, returnUrl);

        // Act
        var result = await _handler.Challenge(request);

        // Assert
        var challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Contains(provider, challengeResult.AuthenticationSchemes);

        _auditLoggerMock.Verify(
            a => a.LogAsync(
                "ExternalAuth",
                "Challenge_Initiated",
                It.Is<object>(o => o.ToString()!.Contains(provider)),
                _dbContext),
            Times.Once);
    }
}
