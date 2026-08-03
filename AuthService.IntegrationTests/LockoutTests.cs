using System.Net;
using System.Net.Http.Json;
using AuthService.Api.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.IntegrationTests;

public sealed class LockoutTests(CustomApiFactory factory) : IClassFixture<CustomApiFactory>
{
    [Fact]
    public async Task RepeatedInvalidPasswords_LockTheAccount()
    {
        var email = $"lockout_{Guid.NewGuid():N}@example.com";
        await factory.CreateConfirmedUserAsync(email, "StrongPassword123!");
        using var client = factory.CreateClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "WrongPassword123!" });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        user!.LockoutEnd.Should().NotBeNull().And.BeAfter(DateTimeOffset.UtcNow);
    }
}
