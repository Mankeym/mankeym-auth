using System.Net;
using AuthService.Api;
using AuthService.Api.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace AuthService.IntegrationTests.Smoke;

public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DB_CONNECTION"] = "Host=localhost;Database=authservice_smoke;Username=postgres;Password=postgres",
                    ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
                    ["Database:ApplyMigrations"] = "false"
                }));

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.RemoveAll(typeof(IDbContextFactory<AppDbContext>));
                services.RemoveAll(typeof(AppDbContext));

                services.AddDbContextFactory<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));

                services.Configure<HealthCheckServiceOptions>(options =>
                {
                    options.Registrations.Clear();
                });

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
