using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.IntegrationTests;

public sealed class AdminAuthorizationTests(CustomApiFactory factory) : IClassFixture<CustomApiFactory>
{
    public static IEnumerable<object[]> ProtectedEndpoints()
    {
        yield return [HttpMethod.Post, "/api/v1/users/00000000-0000-0000-0000-000000000001/roles", "\"User\""];
        yield return [HttpMethod.Delete, "/api/v1/users/00000000-0000-0000-0000-000000000001/roles/User", null!];
        yield return [HttpMethod.Get, "/api/v1/roles", null!];
        yield return [HttpMethod.Get, "/api/v1/roles/User/permissions", null!];
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithoutCredentials_Returns401(HttpMethod method, string path, string? body)
    {
        using var client = CreateClient();
        var response = await client.SendAsync(Request(method, path, body, authenticated: false));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithoutRequiredPermission_Returns403(HttpMethod method, string path, string? body)
    {
        using var client = CreateClient();
        var response = await client.SendAsync(Request(method, path, body, authenticated: true));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient CreateClient() => factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
    {
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = "Test";
            options.DefaultChallengeScheme = "Test";
            options.DefaultForbidScheme = "Test";
        });
    })).CreateClient();

    private static HttpRequestMessage Request(HttpMethod method, string path, string? body, bool authenticated)
    {
        var request = new HttpRequestMessage(method, path);
        if (authenticated) request.Headers.Add("X-Test-Authenticated", "true");
        if (body is not null) request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        return request;
    }
}
