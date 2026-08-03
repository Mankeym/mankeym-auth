using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.IntegrationTests;

public sealed class EmailConfirmationOutboxContractTests(CustomApiFactory factory) : IClassFixture<CustomApiFactory>
{
    [Fact]
    public async Task RequestEmailConfirmation_CreatesExpectedOutboxContract()
    {
        var email = $"confirm_{Guid.NewGuid():N}@example.com";
        await factory.CreateConfirmedUserAsync(email, "StrongPassword123!");

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/request-email-confirmation", new { email });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // A confirmed account intentionally does not create a new confirmation message.
        // Use an unconfirmed account to validate the contract.
        var unconfirmedEmail = $"unconfirmed_{Guid.NewGuid():N}@example.com";
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AuthService.Api.Infrastructure.Persistence.Entities.ApplicationUser>>();
            await users.CreateAsync(new AuthService.Api.Infrastructure.Persistence.Entities.ApplicationUser
            {
                UserName = unconfirmedEmail,
                Email = unconfirmedEmail,
                EmailConfirmed = false
            }, "StrongPassword123!");
        }

        response = await client.PostAsJsonAsync("/api/v1/auth/request-email-confirmation", new { email = unconfirmedEmail });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var message = await factory.GetLatestOutboxMessageAsync("EmailConfirmationEmailRequested");
        message.Should().NotBeNull();

        using var payload = JsonDocument.Parse(message!.Payload);
        payload.RootElement.GetProperty("Email").GetString().Should().Be(unconfirmedEmail);
        payload.RootElement.GetProperty("UserId").GetGuid().Should().NotBeEmpty();
        payload.RootElement.TryGetProperty("ResetLink", out var link).Should().BeTrue();
        link.GetString().Should().StartWith("https://yourapp.com/confirm-email?");
    }
}
