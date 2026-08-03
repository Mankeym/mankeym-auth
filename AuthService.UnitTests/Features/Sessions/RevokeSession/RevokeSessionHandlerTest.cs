using System.Security.Claims;
using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Sessions.RevokeSession;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuthService.UnitTests.Features.Sessions.RevokeSession;

[TestSubject(typeof(RevokeSessionHandler))]
public class RevokeSessionHandlerTest: IDisposable
{
    private readonly Mock<IAuditLogger> _auditLoggerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly AppDbContext _dbContextMock;
    private readonly RevokeSessionHandler _handler;

    public void Dispose()
    {
        _dbContextMock.Database.EnsureDeleted();
        _dbContextMock.Dispose();
    }

    public RevokeSessionHandlerTest()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _auditLoggerMock =  new Mock<IAuditLogger>();
        _dbContextMock = new AppDbContext(options);
        _handler = new RevokeSessionHandler(_userManagerMock.Object, _dbContextMock, _auditLoggerMock.Object);
    }



    [Fact]
    public async Task RevokeSession_Should_Return_Success_When_SessionExists()
    {
        Guid sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ]));

        _userManagerMock.Setup(x => x.GetUserId(claimsPrincipal)).Returns(userId.ToString());

        var session = new UserSession
        {
            Id = sessionId,
            UserId = userId,
            DeviceName = "TestDevice",
            IpHash = "IpHash",
            UserAgentHash = "AgentHash",
            RevokedAtUtc = null
        };

        _dbContextMock.UserSessions.Add(session);
        await _dbContextMock.SaveChangesAsync();

        var result = await _handler.RevokeSessionAsync(sessionId, claimsPrincipal);

        result.Success.Should().BeTrue();


        var updatedSession = await _dbContextMock.UserSessions.FindAsync(sessionId);
        updatedSession.Should().NotBeNull();
        updatedSession!.RevokedAtUtc.Should().NotBeNull();
        updatedSession.RevokeReason.Should().Be("Revoked by user");

        _auditLoggerMock.Verify(x => x.LogAsync(
            "SessionRevoked",
            "Success",
            It.IsAny<object>(),
            _dbContextMock
        ), Times.Once);
    }

    [Fact]
    public async Task RevokeSession_Should_Return_Error_When_SessionNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ]));

        _userManagerMock.Setup(x => x.GetUserId(claimsPrincipal)).Returns(userId.ToString());

        // Act
        var result = await _handler.RevokeSessionAsync(sessionId, claimsPrincipal);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Session not found.");

        _auditLoggerMock.Verify(x => x.LogAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<AppDbContext>()
        ), Times.Never);
    }
}
