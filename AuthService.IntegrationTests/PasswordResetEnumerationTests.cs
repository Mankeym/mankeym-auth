using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests;

public sealed class PasswordResetEnumerationTests(CustomApiFactory factory) : IClassFixture<CustomApiFactory>
{
    [Fact]
    public async Task ForgotPassword_ReturnsTheSameGenericResponse_ForExistingAndUnknownEmail()
    {
        var knownEmail = $"known_{Guid.NewGuid():N}@example.com";
        await factory.CreateConfirmedUserAsync(knownEmail, "StrongPassword123!");
        var unknownEmail = $"unknown_{Guid.NewGuid():N}@example.com";
        using var client = factory.CreateClient();

        var known = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = knownEmail });
        var unknown = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = unknownEmail });

        known.StatusCode.Should().Be(HttpStatusCode.Accepted);
        unknown.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await known.Content.ReadAsStringAsync()).Should().Be(await unknown.Content.ReadAsStringAsync());
    }
}
