using System.Threading.Channels;
using FluentAssertions;
using Nac.Core.Abstractions.Events;
using Nac.EventBus.InMemory;
using Nac.EventBus.Tests.TestHelpers;
using Xunit;

namespace Nac.EventBus.Tests.InMemory;

public class InMemoryEventBusTests
{
    private static Channel<IIntegrationEvent> CreateUnboundedChannel() =>
        Channel.CreateUnbounded<IIntegrationEvent>();

    [Fact]
    public async Task PublishAsync_SingleEvent_WritesToChannel()
    {
        // Arrange
        var channel = CreateUnboundedChannel();
        var bus = new InMemoryEventBus(channel);
        var @event = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "single");

        // Act
        await bus.PublishAsync(@event);

        // Assert — event is readable from the channel
        var success = channel.Reader.TryRead(out var read);
        success.Should().BeTrue();
        read.Should().Be(@event);
    }

    [Fact]
    public async Task PublishAsync_BatchOfEvents_WritesAllToChannel()
    {
        // Arrange
        var channel = CreateUnboundedChannel();
        var bus = new InMemoryEventBus(channel);
        var events = new IIntegrationEvent[]
        {
            new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "first"),
            new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "second"),
            new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "third"),
        };

        // Act
        await bus.PublishAsync(events);

        // Assert — all three events in channel in order
        var read = new List<IIntegrationEvent>();
        while (channel.Reader.TryRead(out var item))
            read.Add(item);

        read.Should().HaveCount(3);
        read.Should().BeEquivalentTo(events, opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task PublishAsync_MultipleSequentialPublishes_AllArrive()
    {
        // Arrange
        var channel = CreateUnboundedChannel();
        var bus = new InMemoryEventBus(channel);

        var e1 = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "a");
        var e2 = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "b");

        // Act
        await bus.PublishAsync(e1);
        await bus.PublishAsync(e2);

        // Assert
        channel.Reader.TryRead(out var r1);
        channel.Reader.TryRead(out var r2);
        r1.Should().Be(e1);
        r2.Should().Be(e2);
    }

    [Fact]
    public async Task PublishAsync_EmptyBatch_WritesNothing()
    {
        // Arrange
        var channel = CreateUnboundedChannel();
        var bus = new InMemoryEventBus(channel);

        // Act
        await bus.PublishAsync(Array.Empty<IIntegrationEvent>());

        // Assert — channel stays empty
        channel.Reader.TryRead(out _).Should().BeFalse();
    }
}
