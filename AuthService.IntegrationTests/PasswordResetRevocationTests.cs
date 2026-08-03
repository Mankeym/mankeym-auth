using System.Net;
using System.Net.Http.Json;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Tokens;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.IntegrationTests;

public sealed class PasswordResetRevocationTests(CustomApiFactory factory) : IClassFixture<CustomApiFactory>
{
    [Fact]
    public async Task SuccessfulReset_RevokesExistingSessionAndRefreshToken()
    {
        var (userId, token) = await factory.CreateUserAndGenerateResetTokenAsync();
        var sessionId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.UserSessions.Add(new UserSession { Id = sessionId, UserId = userId, DeviceName = "test", IpHash = "ip", UserAgentHash = "agent", CreatedAtUtc = DateTime.UtcNow, LastSeenAtUtc = DateTime.UtcNow });
            db.RefreshTokens.Add(new RefreshToken { Id = Guid.NewGuid(), UserId = userId, SessionId = sessionId, TokenHash = TokenSecurityHelper.ComputeSha256Hash("old"), FamilyId = Guid.NewGuid().ToString(), CreatedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(1) });
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { userId, token, newPassword = "NewSecurePassword123!" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var assertScope = factory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await assertDb.UserSessions.SingleAsync(s => s.Id == sessionId)).RevokedAtUtc.Should().NotBeNull();
        (await assertDb.RefreshTokens.SingleAsync(t => t.SessionId == sessionId)).RevokedAtUtc.Should().NotBeNull();
    }
}
