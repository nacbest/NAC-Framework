using FluentAssertions;
using Nac.Testing.Fakes;
using Nac.Testing.Tests.TestHelpers;
using Xunit;

namespace Nac.Testing.Tests.Fakes;

public class FakeEventPublisherTests
{
    [Fact]
    public async Task Publish_CapturesEvent()
    {
        var publisher = new FakeEventPublisher();
        var evt = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "payload");

        await publisher.PublishAsync(evt);

        publisher.PublishedEvents.Should().ContainSingle().Which.Should().Be(evt);
    }

    [Fact]
    public async Task PublishBatch_CapturesAll()
    {
        var publisher = new FakeEventPublisher();
        var events = new[]
        {
            new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "first"),
            new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "second"),
        };

        await publisher.PublishAsync(events);

        publisher.PublishedEvents.Should().HaveCount(2);
    }

    [Fact]
    public void PublishedEvents_InitiallyEmpty()
    {
        var publisher = new FakeEventPublisher();

        publisher.PublishedEvents.Should().BeEmpty();
    }
}
