using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.Login;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Tokens;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuthService.UnitTests.Features.Auth.Login;

[TestSubject(typeof(LoginHandler))]
public class LoginHandlerTest: IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly AppDbContext _dbContextMock;
    private readonly Mock<IAuditLogger> _loggerMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    private readonly LoginHandler _loginHandler;

    public void Dispose()
    {
        _dbContextMock.Database.EnsureDeleted();
        _dbContextMock.Dispose();
    }

    public LoginHandlerTest()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object, contextAccessorMock.Object, claimsFactoryMock.Object, null!, null!, null!, null!);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContextMock = new AppDbContext(options);

        _loggerMock = new Mock<IAuditLogger>();
        _jwtProviderMock = new Mock<IJwtProvider>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _loginHandler = new LoginHandler(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _dbContextMock,
            _loggerMock.Object,
            _jwtProviderMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    [Fact]
    public async Task Login_UserNotFound_ReturnsInvalidLoginResult()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _loginHandler.Login("test@test.com", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsInvalidLoginResult()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@test.com" };

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);

        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "wrong-password", true))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _loginHandler.Login("test@test.com", "wrong-password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_AccountLockedOut_ReturnsLockedOutError()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@test.com" };

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);

        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password", true))
            .ReturnsAsync(SignInResult.LockedOut);

        // Act
        var result = await _loginHandler.Login("test@test.com", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Account is locked out", result.ErrorMessage);

        _loggerMock.Verify(x => x.LogAsync("AccountLocked", "IsLockedOut", It.IsAny<UserLoggedInAuditEvent>(), default), Times.Once);
    }

    [Fact]
    public async Task Login_NotAllowed_ReturnsNotAllowedError()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@test.com" };

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);

        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password", true))
            .ReturnsAsync(SignInResult.NotAllowed);

        // Act
        var result = await _loginHandler.Login("test@test.com", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Login is not allowed", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokensAndSavesSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, Email = "test@test.com" };
        var roles = new List<string> { "User" };
        var expectedToken = "jwt_access_token_123";

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);

        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password", true))
            .ReturnsAsync(SignInResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        _jwtProviderMock.Setup(x => x.GenerateAccessToken(userId, "test@test.com", roles))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _loginHandler.Login("test@test.com", "password");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedToken, result.AccessToken);
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));

        var savedSession = await _dbContextMock.UserSessions.FirstOrDefaultAsync(s => s.UserId == userId);
        savedSession.Should().NotBeNull();
        savedSession.DeviceName.Should().NotBeNullOrEmpty();

        _loggerMock.Verify(x => x.LogAsync("LoginSucceeded", "Success", It.IsAny<UserLoggedInAuditEvent>(), default), Times.Once);
    }
}
