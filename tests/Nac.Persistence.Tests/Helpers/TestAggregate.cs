using Nac.Core.Abstractions.Events;
using Nac.Core.Primitives;

namespace Nac.Persistence.Tests.Helpers;

/// <summary>
/// Test aggregate root for domain event and outbox interceptor tests.
/// </summary>
public class TestAggregate : AggregateRoot<Guid>
{
    public string Name { get; set; } = default!;

    public TestAggregate() : this(Guid.NewGuid(), "") { }

    public TestAggregate(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public void RaiseSampleEvent() => AddDomainEvent(new TestDomainEvent());
    public void RaiseIntegrationEvent() => AddDomainEvent(new TestIntegrationEvent());
}

/// <summary>
/// Test domain event that is not an integration event.
/// </summary>
public sealed record TestDomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Test integration event implementing both IDomainEvent and IIntegrationEvent.
/// </summary>
public sealed record TestIntegrationEvent : IDomainEvent, IIntegrationEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public Guid EventId { get; init; } = Guid.NewGuid();
}
