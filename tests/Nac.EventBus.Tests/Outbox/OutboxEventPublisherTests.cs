using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Events;
using Nac.EventBus.Abstractions;
using Nac.EventBus.Outbox;
using Nac.EventBus.Tests.TestHelpers;
using NSubstitute;
using Xunit;

namespace Nac.EventBus.Tests.Outbox;

public class OutboxEventPublisherTests
{
    private static OutboxEventTypeRegistry BuildRegistry() =>
        new([typeof(SampleIntegrationEvent).Assembly]);

    [Fact]
    public async Task PublishAsync_KnownTypeWithValidJson_ForwardsToEventPublisher()
    {
        // Arrange
        var eventPublisher = Substitute.For<IEventPublisher>();
        var registry = BuildRegistry();
        var logger = Substitute.For<ILogger<OutboxEventPublisher>>();
        var publisher = new OutboxEventPublisher(eventPublisher, registry, logger);

        var original = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "outbox-payload");
        var payload = JsonSerializer.Serialize(original);
        var eventType = typeof(SampleIntegrationEvent).AssemblyQualifiedName!;

        // Act
        await publisher.PublishAsync(eventType, payload);

        // Assert
        await eventPublisher.Received(1).PublishAsync(
            Arg.Is<IIntegrationEvent>(e =>
                e is SampleIntegrationEvent && ((SampleIntegrationEvent)e).Data == "outbox-payload"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_UnknownType_LogsErrorAndDoesNotForward()
    {
        // Arrange
        var eventPublisher = Substitute.For<IEventPublisher>();
        var registry = BuildRegistry();
        var logger = Substitute.For<ILogger<OutboxEventPublisher>>();
        var publisher = new OutboxEventPublisher(eventPublisher, registry, logger);

        // Act
        await publisher.PublishAsync("Nac.Unknown.GhostEvent", "{}");

        // Assert — nothing forwarded
        await eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());

        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task PublishAsync_InvalidJson_LogsErrorAndDoesNotForward()
    {
        // Arrange
        var eventPublisher = Substitute.For<IEventPublisher>();
        var registry = BuildRegistry();
        var logger = Substitute.For<ILogger<OutboxEventPublisher>>();
        var publisher = new OutboxEventPublisher(eventPublisher, registry, logger);
        var eventType = typeof(SampleIntegrationEvent).AssemblyQualifiedName!;

        // Act
        await publisher.PublishAsync(eventType, "not-valid-json{{{{");

        // Assert
        await eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());

        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(ex => ex is JsonException),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task PublishAsync_NullJsonPayload_LogsErrorAndDoesNotForward()
    {
        // Arrange — JSON literal "null" deserialises to null; the `as IIntegrationEvent` cast
        // produces null, triggering the null-guard log path.
        var eventPublisher = Substitute.For<IEventPublisher>();
        var registry = BuildRegistry();
        var logger = Substitute.For<ILogger<OutboxEventPublisher>>();
        var publisher = new OutboxEventPublisher(eventPublisher, registry, logger);
        var eventType = typeof(SampleIntegrationEvent).AssemblyQualifiedName!;

        // Act
        await publisher.PublishAsync(eventType, "null");

        // Assert — null deserialization logged, nothing forwarded
        await eventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());

        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task PublishAsync_CancellationTokenPropagatedToEventPublisher()
    {
        // Arrange
        var eventPublisher = Substitute.For<IEventPublisher>();
        var registry = BuildRegistry();
        var logger = Substitute.For<ILogger<OutboxEventPublisher>>();
        var publisher = new OutboxEventPublisher(eventPublisher, registry, logger);

        var original = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "ct-test");
        var payload = JsonSerializer.Serialize(original);
        var eventType = typeof(SampleIntegrationEvent).AssemblyQualifiedName!;

        using var cts = new CancellationTokenSource();

        // Act
        await publisher.PublishAsync(eventType, payload, cts.Token);

        // Assert — the specific token was forwarded
        await eventPublisher.Received(1).PublishAsync(
            Arg.Any<IIntegrationEvent>(),
            cts.Token);
    }
}
