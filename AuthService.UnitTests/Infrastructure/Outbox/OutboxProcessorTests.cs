using AuthService.Api.Infrastructure.Outbox;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AuthService.UnitTests.Infrastructure.Outbox;

public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task TemporaryFailure_IsRetriedWithTheSameMessageId_AndOnlySuccessMarksProcessed()
    {
        var transport = new Mock<IOutboxTransport>();
        var deliveryIds = new List<Guid>();
        transport.Setup(x => x.DeliverAsync(It.IsAny<OutboxDelivery>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxDelivery, CancellationToken>((delivery, _) => deliveryIds.Add(delivery.MessageId))
            .ThrowsAsync(new InvalidOperationException("SMTP password=secret"));

        var services = new ServiceCollection();
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>(x => x.UseInMemoryDatabase(databaseName, databaseRoot));
        services.AddScoped<IOutboxTransport>(_ => transport.Object);
        await using var provider = services.BuildServiceProvider();
        var processor = new OutboxProcessor(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance);

        Guid messageId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var message = new OutboxMessage { Id = Guid.NewGuid(), Type = "PasswordResetEmailRequested", Payload = "{}", OccurredAtUtc = DateTime.UtcNow };
            messageId = message.Id;
            db.OutboxMessages.Add(message);
            await db.SaveChangesAsync();
        }

        await processor.ProcessPendingMessagesAsync(CancellationToken.None);

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var message = await db.OutboxMessages.SingleAsync();
            message.ProcessedAtUtc.Should().BeNull();
            message.Attempts.Should().Be(1);
            message.NextAttemptAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(1), TimeSpan.FromSeconds(2));
            message.Error.Should().Be("Delivery failed.");
            message.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        transport.Setup(x => x.DeliverAsync(It.IsAny<OutboxDelivery>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxDelivery, CancellationToken>((delivery, _) => deliveryIds.Add(delivery.MessageId))
            .Returns(Task.CompletedTask);
        await processor.ProcessPendingMessagesAsync(CancellationToken.None);

        await using var finalScope = provider.CreateAsyncScope();
        var finalMessage = await finalScope.ServiceProvider.GetRequiredService<AppDbContext>().OutboxMessages.SingleAsync();
        finalMessage.ProcessedAtUtc.Should().NotBeNull();
        deliveryIds.Should().Equal(messageId, messageId);
    }
}
