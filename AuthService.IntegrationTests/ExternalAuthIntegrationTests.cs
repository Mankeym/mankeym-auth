using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthService.IntegrationTests;

public class ExternalAuthIntegrationTests(CustomApiFactory factory) : IClassFixture<CustomApiFactory>
{
    [Fact]
    public async Task ExternalCallback_Endpoint_RedirectsToLogin_WhenNoExternalCookie()
    {
        // Arrange
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/auth/external/google/callback");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
