using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace AuthService.IntegrationTests;

public sealed class AuthorizationMatrixTests(CustomApiFactory factory) : IClassFixture<CustomApiFactory>
{
    [Theory]
    [InlineData("profile.read", "/api/v1/roles", HttpStatusCode.Forbidden)]
    [InlineData("audit.read", "/api/v1/admin/outbox/failed", HttpStatusCode.OK)]
    [InlineData("roles.read", "/api/v1/roles", HttpStatusCode.OK)]
    [InlineData("users.manage", "/api/v1/roles", HttpStatusCode.Forbidden)]
    public async Task RolePermissionMatrix_EnforcesExpectedAccess(string permissions, string path, HttpStatusCode expected)
    {
        using var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Test-Authenticated", "true");
        request.Headers.Add("X-Test-Permissions", permissions);

        (await client.SendAsync(request)).StatusCode.Should().Be(expected);
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
}
