using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests;

public sealed class AuthSessionFlowTests(CustomApiFactory factory) : IClassFixture<CustomApiFactory>
{
    [Fact]
    public async Task Login_Refresh_Logout_RevokesTheRefreshToken()
    {
        var email = $"flow_{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";
        await factory.CreateConfirmedUserAsync(email, password);

        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();

        var firstRefreshToken = ExtractRefreshCookie(login);
        var refresh = await SendWithRefreshCookie(client, "/api/v1/auth/refresh", firstRefreshToken);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        (await refresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();

        var secondRefreshToken = ExtractRefreshCookie(refresh);
        var logout = await SendWithRefreshCookie(client, "/api/v1/auth/logout", secondRefreshToken);
        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterLogoutRefresh = await SendWithRefreshCookie(client, "/api/v1/auth/refresh", secondRefreshToken);
        afterLogoutRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<HttpResponseMessage> SendWithRefreshCookie(HttpClient client, string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", $"refreshToken={token}");
        return await client.SendAsync(request);
    }

    private static string ExtractRefreshCookie(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("refreshToken=", StringComparison.Ordinal));
        return cookie.Split(';', 2)[0].Split('=', 2)[1];
    }
}
