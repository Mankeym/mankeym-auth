using AuthService.Api.Features.Audit;
using AuthService.Api.Features.Auth.Refresh;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Tokens;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AuthService.IntegrationTests;

public class ParallelRefreshIntegrationTests : IClassFixture<CustomApiFactory>
{
    private readonly CustomApiFactory _factory;

    public ParallelRefreshIntegrationTests(CustomApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ParallelRefreshRequests_WithinWindow_ShouldTriggerParallelProtection()
    {
        // Arrange
        using var scopeSetup = _factory.Services.CreateScope();
        var dbContextSetup = scopeSetup.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManagerSetup = scopeSetup.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = $"testuser_{Guid.NewGuid()}@test.com", Email = $"testuser_{Guid.NewGuid()}@test.com" };

        dbContextSetup.Users.Add(user);

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceName = "TestDevice",
            IpHash = "IpHash",
            UserAgentHash = "AgentHash",
            RevokedAtUtc = null
        };
        dbContextSetup.UserSessions.Add(session);

        var rawToken = "initial-refresh-token-parallel";
        var tokenHash = TokenSecurityHelper.ComputeSha256Hash(rawToken);
        var familyId = Guid.NewGuid().ToString();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RevokedAtUtc = null,
            UsedAtUtc = null
        };
        dbContextSetup.RefreshTokens.Add(refreshToken);
        await dbContextSetup.SaveChangesAsync();

        // Создаем ДВА НЕЗАВИСИМЫХ SCOPE для двух параллельных запросов (как в реальном Web API)
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();

        var dbContext1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager1 = scope1.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var dbContext2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager2 = scope2.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var jwtProviderMock = new Mock<IJwtProvider>();
        jwtProviderMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync("new-access-token");

        var handler1 = new RefreshHandler(dbContext1, jwtProviderMock.Object, new Mock<IAuditLogger>().Object, userManager1);
        var handler2 = new RefreshHandler(dbContext2, jwtProviderMock.Object, new Mock<IAuditLogger>().Object, userManager2);

        // Act: Запускаем параллельно два независимых хэндлера с отдельными DbContext
        var task1 = handler1.RefreshTokensAsync(rawToken);
        var task2 = handler2.RefreshTokensAsync(rawToken);

        var results = await Task.WhenAll(task1, task2);

        // Assert
        results.Any(r => r.Success).Should().BeTrue();
        results.Any(r => !r.Success).Should().BeTrue();
    }
}
