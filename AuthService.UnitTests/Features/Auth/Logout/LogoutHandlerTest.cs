using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.Logout;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Tokens;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuthService.UnitTests.Features.Auth.Logout;

[TestSubject(typeof(LogoutHandler))]
public class LogoutHandlerTest : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IAuditLogger> _auditLoggerMock;
    private readonly LogoutHandler _handler;

    public LogoutHandlerTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);

        _auditLoggerMock = new Mock<IAuditLogger>();

        _handler = new LogoutHandler(_dbContext, _auditLoggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task LogoutAsync_EmptyOrNullToken_ReturnsSuccess_And_DoesNothing(string? rawToken)
    {
        // Act
        var result = await _handler.LogoutAsync(rawToken);

        // Assert
        Assert.True(result.Success);

        _auditLoggerMock.Verify(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<AppDbContext>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_TokenNotFound_ReturnsSuccess_And_DoesNothing()
    {
        // Act
        var result = await _handler.LogoutAsync("non-existent-token");

        // Assert
        Assert.True(result.Success);

        _auditLoggerMock.Verify(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<AppDbContext>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesSession_And_FamilyTokens_And_LogsAudit()
    {
        // Arrange
        var rawToken = "valid-logout-token";
        var tokenHash = TokenSecurityHelper.ComputeSha256Hash(rawToken);
        var familyId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceName = "TestDevice",
            IpHash = "IpHash",
            UserAgentHash = "AgentHash",
            RevokedAtUtc = null
        };

        var tokenToLogout = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            RevokedAtUtc = null
        };

        var otherTokenInFamily = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = userId,
            TokenHash = "some-other-old-hash",
            FamilyId = familyId,
            RevokedAtUtc = null
        };

        _dbContext.UserSessions.Add(session);
        _dbContext.RefreshTokens.AddRange(tokenToLogout, otherTokenInFamily);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.LogoutAsync(rawToken);

        // Assert
        Assert.True(result.Success);

        var updatedSession = await _dbContext.UserSessions.FindAsync(session.Id);
        var updatedTokens = await _dbContext.RefreshTokens.Where(t => t.FamilyId == familyId).ToListAsync();

        Assert.NotNull(updatedSession!.RevokedAtUtc);
        Assert.Equal("User logout", updatedSession.RevokeReason);

        Assert.All(updatedTokens, t => Assert.NotNull(t.RevokedAtUtc));


        _auditLoggerMock.Verify(x => x.LogAsync(
            "UserLogout",
            "Success",
            It.IsAny<object>(), _dbContext), Times.Once);
    }
}
