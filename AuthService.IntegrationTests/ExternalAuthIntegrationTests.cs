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
            AllowAutoRedirect = false // Чтобы поймать 302 редирект, а не следовать за ним
        });

        // Act
        // Теперь запрос пойдет в приложение, поднятое через ваш CustomApiFactory
        var response = await client.GetAsync("/api/v1/auth/external/google/callback");

        // Assert
        // Ожидаем редирект (302), а не 404 NotFound
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
