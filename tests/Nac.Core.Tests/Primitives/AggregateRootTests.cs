using FluentAssertions;
using Nac.Core.Primitives;
using Xunit;

namespace Nac.Core.Tests.Primitives;

public class AggregateRootTests
{
    private sealed class TestDomainEvent : IDomainEvent
    {
        public string EventName { get; }
        public DateTime OccurredOn { get; }

        public TestDomainEvent(string eventName, DateTime? occurredOn = null)
        {
            EventName = eventName;
            OccurredOn = occurredOn ?? DateTime.UtcNow;
        }
    }

    private sealed class TestAggregateRoot : AggregateRoot<int>
    {
        public string Name { get; set; } = string.Empty;

        public TestAggregateRoot(int id, string name = "")
        {
            Id = id;
            Name = name;
        }

        public void RaiseDomainEvent(IDomainEvent @event)
        {
            AddDomainEvent(@event);
        }
    }

    [Fact]
    public void DomainEvents_InitiallyEmpty()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(1);

        // Act & Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddDomainEvent_AddsEventToDomainEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(1);
        var @event = new TestDomainEvent("TestEvent");

        // Act
        aggregate.RaiseDomainEvent(@event);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents.First().Should().Be(@event);
    }

    [Fact]
    public void AddDomainEvent_WithMultipleEvents_AddsAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(1);
        var event1 = new TestDomainEvent("Event1");
        var event2 = new TestDomainEvent("Event2");
        var event3 = new TestDomainEvent("Event3");

        // Act
        aggregate.RaiseDomainEvent(event1);
        aggregate.RaiseDomainEvent(event2);
        aggregate.RaiseDomainEvent(event3);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(3);
        aggregate.DomainEvents.Should().ContainInOrder(event1, event2, event3);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(1);
        aggregate.RaiseDomainEvent(new TestDomainEvent("Event1"));
        aggregate.RaiseDomainEvent(new TestDomainEvent("Event2"));

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_ReturnsReadOnlyList()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(1);
        aggregate.RaiseDomainEvent(new TestDomainEvent("Event"));

        // Act & Assert
        var events = aggregate.DomainEvents;
        events.Should().BeAssignableTo<IReadOnlyList<IDomainEvent>>();
    }

    [Fact]
    public void InheritsEntityEquality()
    {
        // Arrange
        var aggregate1 = new TestAggregateRoot(1, "Test");
        var aggregate2 = new TestAggregateRoot(1, "Different");

        // Act & Assert
        (aggregate1 == aggregate2).Should().BeTrue();
    }

    [Fact]
    public void ClearDomainEvents_CanAddEventsAfterClearing()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(1);
        aggregate.RaiseDomainEvent(new TestDomainEvent("Event1"));
        aggregate.ClearDomainEvents();
        var newEvent = new TestDomainEvent("Event2");

        // Act
        aggregate.RaiseDomainEvent(newEvent);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents.First().Should().Be(newEvent);
    }

    [Fact]
    public void MultipleAggregates_MaintainIndependentEventLists()
    {
        // Arrange
        var aggregate1 = new TestAggregateRoot(1);
        var aggregate2 = new TestAggregateRoot(2);
        var event1 = new TestDomainEvent("Event1");
        var event2 = new TestDomainEvent("Event2");

        // Act
        aggregate1.RaiseDomainEvent(event1);
        aggregate2.RaiseDomainEvent(event2);

        // Assert
        aggregate1.DomainEvents.Should().HaveCount(1).And.Contain(event1);
        aggregate2.DomainEvents.Should().HaveCount(1).And.Contain(event2);
    }

    [Fact]
    public void DomainEvent_PreserveOccurredOnDate()
    {
        // Arrange
        var occurredOn = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var @event = new TestDomainEvent("Event", occurredOn);
        var aggregate = new TestAggregateRoot(1);

        // Act
        aggregate.RaiseDomainEvent(@event);

        // Assert
        aggregate.DomainEvents.First().OccurredOn.Should().Be(occurredOn);
    }
}
