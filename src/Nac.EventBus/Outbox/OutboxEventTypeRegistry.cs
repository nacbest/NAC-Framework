using System.Collections.Frozen;
using System.Reflection;
using Nac.Core.Abstractions.Events;

namespace Nac.EventBus.Outbox;

/// <summary>
/// Scans assemblies for IIntegrationEvent implementations and builds
/// an allowlist mapping type name -> Type.
/// Prevents arbitrary type resolution from untrusted outbox data.
/// </summary>
internal sealed class OutboxEventTypeRegistry
{
    private readonly FrozenDictionary<string, Type> _knownTypes;

    internal OutboxEventTypeRegistry(IReadOnlyList<Assembly> assemblies)
    {
        var types = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var assembly in assemblies)
        {
            var eventTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                         && typeof(IIntegrationEvent).IsAssignableFrom(t));

            foreach (var type in eventTypes)
            {
                var key = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
                types.TryAdd(key, type);
                // Also register by FullName for resilience
                if (type.FullName is not null)
                    types.TryAdd(type.FullName, type);
            }
        }

        _knownTypes = types.ToFrozenDictionary();
    }

    internal Type? Resolve(string eventTypeName) =>
        _knownTypes.GetValueOrDefault(eventTypeName);
}
