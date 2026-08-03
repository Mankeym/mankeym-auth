using System.Security.Claims;
using AuthService.Api.Features.Users.GetMe;
using AuthService.Api.Infrastructure.Persistence.Entities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;

namespace AuthService.UnitTests.Features.Users.GetMe;

[TestSubject(typeof(GetMeHandler))]
public class GetMeHandlerTest
{

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly GetMeHandler _handler;

    public GetMeHandlerTest()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _handler = new GetMeHandler(_userManagerMock.Object);
    }

    [Fact]
    public async Task GetMe_Should_Return_User_When_GetUserAsync_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new ApplicationUser
        {
            Id = userId,
            Email = "test@test.com",
            CreatedAtUTC = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUTC = DateTime.UtcNow
        };
        var expectedRoles = new List<string> { "User", "Admin" };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ]));

        _userManagerMock.Setup(x => x.GetUserAsync(claimsPrincipal))
            .ReturnsAsync(expectedUser);

        _userManagerMock.Setup(x => x.GetRolesAsync(expectedUser))
            .ReturnsAsync(expectedRoles);

        // Act
        var result = await _handler.GetMe(claimsPrincipal);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal(expectedUser.Email, result.User.Email);
        Assert.Equal(expectedRoles, result.User.Roles);
        Assert.Equal(expectedUser.CreatedAtUTC, result.User.CreatedAt);
        Assert.Equal(expectedUser.UpdatedAtUTC, result.User.UpdatedAt);
    }

    [Fact]
    public async Task GetMe_Should_FallbackToFindById_When_GetUserAsync_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var expectedUser = new ApplicationUser
        {
            Id = Guid.Parse(userId),
            Email = "fallback@test.com"
        };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, userId) // Имитируем ID в claim Sub
        ]));

        // GetUserAsync возвращает null
        _userManagerMock.Setup(x => x.GetUserAsync(claimsPrincipal))
            .ReturnsAsync((ApplicationUser)null!);

        // Но FindByIdAsync находит пользователя
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        _userManagerMock.Setup(x => x.GetRolesAsync(expectedUser))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _handler.GetMe(claimsPrincipal);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("fallback@test.com", result.User.Email);

        // Проверяем, что оба метода были вызваны
        _userManagerMock.Verify(x => x.GetUserAsync(claimsPrincipal), Times.Once);
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetMe_Should_Return_Unauthorized_When_UserNotFound()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        _userManagerMock.Setup(x => x.GetUserAsync(claimsPrincipal))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _handler.GetMe(claimsPrincipal);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Unauthorized", result.ErrorMessage);
        Assert.Null(result.User);
    }
}
