using AuthService.Api.Features.Auth.Register;
using FluentValidation.TestHelper;

namespace AuthService.UnitTests.Features.Auth.Register;

public class RegisterRequestValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Is_Invalid()
    {
        // Arrange
        var request = new RegisterRequest { Email = "not-an-email", Password = "StrongPassword123!" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Invalid email format.");
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Too_Common()
    {
        var request = new RegisterRequest { Email = "test@test.com", Password = "Password123!" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("This password is too common and insecure.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Request_Is_Valid()
    {
        var request = new RegisterRequest { Email = "admin@test.com", Password = "MySuperSecretPassword123!" };

        var result = _validator.TestValidate(request);

        // Убеждаемся, что ошибок валидации нет вообще
        result.ShouldNotHaveAnyValidationErrors();
    }
}
