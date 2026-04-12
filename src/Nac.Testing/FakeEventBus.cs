using Nac.Abstractions.Messaging;

namespace Nac.Testing;

/// <summary>
/// In-memory <see cref="IEventBus"/> for tests. Captures all published events
/// so tests can assert on what was published without a real broker.
/// </summary>
public sealed class FakeEventBus : IEventBus
{
    private readonly List<IIntegrationEvent> _published = [];

    /// <summary>All events published during the test.</summary>
    public IReadOnlyList<IIntegrationEvent> PublishedEvents => _published;

    public Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        _published.Add(@event);
        return Task.CompletedTask;
    }

    /// <summary>Returns published events of a specific type.</summary>
    public IReadOnlyList<T> PublishedOf<T>() where T : IIntegrationEvent
        => _published.OfType<T>().ToList();

    /// <summary>Clears all captured events.</summary>
    public void Clear() => _published.Clear();
}
