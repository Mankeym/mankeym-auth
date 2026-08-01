using System.Net;
using System.Net.Http.Json;
using AuthService.Api.Features.Auth.Register;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests;

// IClassFixture гарантирует, что CustomApiFactory (и БД) создастся один раз для всего класса
public class RegisterEndpointTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;

    public RegisterEndpointTests(CustomApiFactory factory)
    {
        // CreateClient() запускает тестовый сервер в памяти
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOkAndCreatesUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "integration@test.com",
            Password = "StrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RegistrationResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.UserId.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange (email без @)
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "StrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Убеждаемся, что FluentValidation отработал корректно
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Invalid email format.");
    }
}
