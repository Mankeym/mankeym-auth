using System.Net;
using System.Net.Http.Json;
using AuthService.Api.Features.Auth.Register;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests;

public class RegisterEndpointTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;

    public RegisterEndpointTests(CustomApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOkAndCreatesUser()
    {
        var request = new RegisterRequest
        {
            Email = "integration@test.com",
            Password = "StrongPassword123!"
        };

        var response = await _client.PostAsJsonAsync("api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RegistrationResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.UserId.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "StrongPassword123!"
        };

        var response = await _client.PostAsJsonAsync("api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Invalid email format.");
    }
}
