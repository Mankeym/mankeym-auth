using System.Net;
using System.Net.Http.Json;
using AuthService.Api.Features.Auth.Login;
using FluentAssertions;

namespace AuthService.IntegrationTests;

public sealed class RedisRateLimitTests(CustomApiFactory factory) : IClassFixture<CustomApiFactory>
{
    [Fact]
    public async Task Login_AfterFiveAttempts_ReturnsTooManyRequests()
    {
        using var client = factory.CreateClient();
        var request = new LoginRequest
        {
            Email = $"rate-limit-{Guid.NewGuid():N}@example.com",
            Password = "WrongPassword123"
        };

        var responses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var response = await client.PostAsJsonAsync("api/v1/auth/login", request);
            responses.Add(response.StatusCode);
        }

        responses.Take(5).Should().OnlyContain(status => status == HttpStatusCode.BadRequest);
        responses[5].Should().Be(HttpStatusCode.TooManyRequests);
    }
}
