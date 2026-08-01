using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using FluentAssertions;
using System.Net;
using AuthService.Api;
using AuthService.Api.Infrastructure.Persistence;

namespace AuthService.IntegrationTests.Smoke;

public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 1. Заменяем реальный DbContext на InMemory
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));

                // 2. Очищаем все существующие регистрации HealthChecks через опции
                services.Configure<HealthCheckServiceOptions>(options =>
                {
                    options.Registrations.Clear();
                });

                // 3. Добавляем одну проверку с уникальным именем
                services.AddHealthChecks()
                    .AddCheck("test_core", () => HealthCheckResult.Healthy(), tags: new[] { "live", "ready" });
            });
        });
    }

    [Fact]
    public async Task HealthLiveEndpoint_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReadyEndpoint_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
