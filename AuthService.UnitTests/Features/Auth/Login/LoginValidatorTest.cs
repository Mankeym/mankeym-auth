using AuthService.Api.Features.Auth.Login;
using FluentValidation.TestHelper;

namespace AuthService.UnitTests.Features.Auth.Login;

public class LoginValidatorTest
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Is_Invalid()
    {
        // Arrange
        var request = new LoginRequest { Email = "not-an-email", Password = "StrongPassword123!" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Invalid email format.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Request_Is_Valid()
    {
        var request = new LoginRequest { Email = "admin@test.com", Password = "MySuperSecretPassword123!" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
