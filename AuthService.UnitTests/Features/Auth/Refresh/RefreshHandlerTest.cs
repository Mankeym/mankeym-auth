using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.Refresh;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuthService.UnitTests.Features.Auth.Refresh;

public class RefreshHandlerTest : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IAuditLogger> _auditLoggerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;

    private readonly RefreshHandler _handler;

    public RefreshHandlerTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);

        _jwtProviderMock = new Mock<IJwtProvider>();
        _auditLoggerMock = new Mock<IAuditLogger>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _handler = new RefreshHandler(
            _dbContext,
            _jwtProviderMock.Object,
            _auditLoggerMock.Object,
            _userManagerMock.Object,
            _permissionRepositoryMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task RefreshTokensAsync_TokenNotFound_ReturnsError()
    {
        // Act
        var result = await _handler.RefreshTokensAsync("non-existent-token");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Token not found.", result.Error);
    }

    [Fact]
    public async Task RefreshTokensAsync_SessionRevoked_ReturnsError()
    {
        // Arrange
        var rawToken = "valid-raw-token";
        var tokenHash = TokenSecurityHelper.ComputeSha256Hash(rawToken);

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            RevokedAtUtc = DateTime.UtcNow,
            DeviceName = "TestDevice",
            IpHash = "TestIpHash",
            UserAgentHash = "TestAgentHash"
        };
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            Session = session,
            User = new ApplicationUser()
        };

        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.RefreshTokensAsync(rawToken);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Session has been terminated.", result.Error);
    }

    [Fact]
    public async Task RefreshTokensAsync_TokenExpired_ReturnsError()
    {
        // Arrange
        var rawToken = "expired-raw-token";
        var tokenHash = TokenSecurityHelper.ComputeSha256Hash(rawToken);

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            DeviceName = "TestDevice",
            IpHash = "TestIpHash",
            UserAgentHash = "TestAgentHash"
        };
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            Session = session,
            User = new ApplicationUser()
        };

        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.RefreshTokensAsync(rawToken);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Token has expired.", result.Error);
    }

    [Fact]
    public async Task RefreshTokensAsync_ValidToken_ReturnsNewTokensAndUpdatesDb()
    {
        // Arrange
        var rawToken = "valid-raw-token";
        var tokenHash = TokenSecurityHelper.ComputeSha256Hash(rawToken);
        var expectedAccessToken = "new_jwt_access_token";

        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@test.com" };
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DeviceName = "TestDevice",
            IpHash = "TestIpHash",
            UserAgentHash = "TestAgentHash"
        };
        var familyId = Guid.NewGuid().ToString();

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            FamilyId = familyId,
            SessionId = session.Id,
            UserId = user.Id,
            Session = session,
            User = user
        };

        _dbContext.Users.Add(user);
        _dbContext.UserSessions.Add(session);
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();

        var roles = new List<string> { "User" };
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

        _jwtProviderMock.Setup(x => x.GenerateAccessToken(user.Id, user.Email, roles, It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .ReturnsAsync(expectedAccessToken);

        // Act
        var result = await _handler.RefreshTokensAsync(rawToken);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedAccessToken, result.AccessToken);
        Assert.NotNull(result.RefreshToken);

        var dbToken = await _dbContext.RefreshTokens.FindAsync(token.Id);
        var newTokensCount = await _dbContext.RefreshTokens.CountAsync();

        Assert.NotNull(dbToken!.UsedAtUtc);
        Assert.Equal(2, newTokensCount);
    }
}
