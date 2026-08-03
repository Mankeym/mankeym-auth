using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AuthService.Api.Features.Sessions.GetMySessions;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuthService.UnitTests.Features.Sessions.GetMySessions;

[TestSubject(typeof(GetMySessionsHandler))]
public class GetMySessionsHandlerTest : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly GetMySessionsHandler _handler;
    private readonly AppDbContext _dbContextMock;

    public GetMySessionsHandlerTest()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContextMock = new AppDbContext(options);

        _handler = new GetMySessionsHandler(_userManagerMock.Object, _dbContextMock);
    }

    public void Dispose()
    {
        _dbContextMock.Database.EnsureDeleted();
        _dbContextMock.Dispose();
    }

    [Fact]
    public async Task GetMySessions_Should_Return_Sessions_When_UserIsFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ]));

        _userManagerMock.Setup(x => x.GetUserId(claimsPrincipal)).Returns(userId.ToString());

        _dbContextMock.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceName = "TestDevice1",
            UserAgentHash = "Hash1",
            IpHash = "Ip1",
            LastSeenAtUtc = DateTime.UtcNow
        });

        _dbContextMock.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceName = "TestDevice2",
            UserAgentHash = "Hash2",
            IpHash = "Ip2",
            LastSeenAtUtc = DateTime.UtcNow.AddHours(-1)
        });

        await _dbContextMock.SaveChangesAsync();

        // Act
        var result = await _handler.GetMySessions(claimsPrincipal);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Sessions);
        Assert.Equal(2, result.Sessions.Count);
    }

    [Fact]
    public async Task GetMySessions_Should_ReturnError_When_GetUserId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, userId)
        ]));

        _userManagerMock.Setup(x => x.GetUserId(claimsPrincipal)).Returns((string)null!);

        // Act
        var result = await _handler.GetMySessions(claimsPrincipal);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found or unauthorized.", result.ErrorMessage);
    }

    [Fact]
    public async Task GetMySessions_Should_Return_Unauthorized_When_UserIdIsEmpty()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        _userManagerMock.Setup(x => x.GetUserId(claimsPrincipal))
            .Returns(string.Empty);

        // Act
        var result = await _handler.GetMySessions(claimsPrincipal);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found or unauthorized.", result.ErrorMessage);
        Assert.Null(result.Sessions);
    }
}
