using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.Register;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AuthService.UnitTests.Features.Auth.Register;

public class RegisterHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IAuditLogger> _loggerMock;
    private readonly AppDbContext _dbContext;
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        // Настраиваем "заглушку" для UserManager (ему нужен IUserStore в конструкторе)
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _loggerMock = new Mock<IAuditLogger>();
        _dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        // Передаем нашу заглушку в реальный хендлер
        _handler = new RegisterHandler(_userManagerMock.Object, _dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUser_Should_Return_Success_When_Identity_Succeeds()
    {
        // Arrange: Говорим моку возвращать "Success" при создании юзера и выдаче роли
        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.CreateUser("test@test.com", "ValidPass123!");

        // Assert (используем FluentAssertions)
        result.Success.Should().BeTrue();
        result.UserId.Should().NotBeNull();

        // Проверяем, что метод AddToRoleAsync действительно был вызван 1 раз с ролью "User"
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
    }

    [Fact]
    public async Task CreateUser_Should_Return_Errors_When_Creation_Fails()
    {
        // Arrange: Имитируем ошибку от Identity (например, email уже занят)
        var identityError = new IdentityError { Description = "Email is already taken" };

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _handler.CreateUser("exist@test.com", "ValidPass123!");

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Email is already taken");
        result.UserId.Should().BeNull();

        // Проверяем, что если создание упало, роль НЕ выдавалась
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }
}
