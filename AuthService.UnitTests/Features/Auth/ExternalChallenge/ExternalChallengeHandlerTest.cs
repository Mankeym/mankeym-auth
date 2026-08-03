using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.ExternalChallenge;
using AuthService.Api.Infrastructure.Persistence.Entities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace AuthService.UnitTests.Features.Auth.ExternalChallenge;

[TestSubject(typeof(ExternalChallengeHandler))]
public class ExternalChallengeHandlerTests
{
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<LinkGenerator> _linkGeneratorMock;
    private readonly Mock<IHttpContextAccessor> _contextAccessorMock;
    private readonly Mock<IAuditLogger> _auditLoggerMock;
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

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "Test-Agent";
        _contextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _handler = new ExternalChallengeHandler(
            _signInManagerMock.Object,
            _linkGeneratorMock.Object,
            _auditLoggerMock.Object,
            _contextAccessorMock.Object
        );
    }

    [Fact]
    public async Task Challenge_WhenCalled_LogsAuditAndReturnsChallengeResult()
    {
        // Arrange
        string provider = "Google";
        string returnUrl = "/dashboard";

        _linkGeneratorMock
            .Setup(l => l.GetPathByAction(
                It.IsAny<HttpContext?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<PathString?>(),
                It.IsAny<FragmentString>(),
                It.IsAny<LinkOptions?>()))
            .Returns("/api/v1/auth/external/google/callback");

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
                default),
            Times.Once);
    }
}
