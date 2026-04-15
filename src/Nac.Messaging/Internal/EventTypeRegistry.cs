using System.Collections.Concurrent;
using Nac.Core.Messaging;

namespace Nac.Messaging.Internal;

/// <summary>
/// Maps integration event type names (from <see cref="IIntegrationEvent.EventType"/>)
/// to CLR types. Populated at startup during handler scanning; used by the
/// outbox worker to deserialize outbox messages.
/// </summary>
internal sealed class EventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> _types = new();

    public void Register(Type eventType)
    {
        var key = eventType.FullName
            ?? throw new ArgumentException($"Event type {eventType} has no FullName.");
        _types.TryAdd(key, eventType);
    }

    public Type? Resolve(string eventTypeName)
        => _types.GetValueOrDefault(eventTypeName);
}
