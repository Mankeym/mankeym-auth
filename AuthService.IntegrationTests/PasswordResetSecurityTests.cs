using System.Net;
using System.Net.Http.Json;
using AuthService.Api.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.IntegrationTests;

public class PasswordResetEndpointTests : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client;
    private readonly CustomApiFactory _factory;

    public PasswordResetEndpointTests(CustomApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ForgotPassword_WithExistingConfirmedEmail_ReturnsAcceptedAndQueuesOutboxMessage()
    {
        // Arrange
        var email = "forgot-test@example.com";
        await _factory.CreateConfirmedUserAsync(email, "StrongPassword123!");

        var request = new { Email = email };

        // Act
        var response = await _client.PostAsJsonAsync("api/v1/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var outboxMessageExists = await _factory.HasOutboxMessageOfTypeAsync("PasswordResetEmailRequested");
        outboxMessageExists.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_WithAlteredToken_ReturnsBadRequest()
    {
        // Arrange
        var (userId, validToken) = await _factory.CreateUserAndGenerateResetTokenAsync();
        var alteredToken = validToken + "tampered";

        var request = new
        {
            UserId = userId,
            Token = alteredToken,
            NewPassword = "NewSecurePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ReusedToken_FailsOnSecondAttempt()
    {
        // Arrange
        var (userId, validToken) = await _factory.CreateUserAndGenerateResetTokenAsync();

        var request = new
        {
            UserId = userId,
            Token = validToken,
            NewPassword = "NewSecurePassword123!"
        };

        var firstResponse = await _client.PostAsJsonAsync("api/v1/auth/reset-password", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var secondResponse = await _client.PostAsJsonAsync("api/v1/auth/reset-password", request);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
