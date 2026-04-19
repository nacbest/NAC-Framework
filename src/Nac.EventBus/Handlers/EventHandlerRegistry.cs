using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nac.EventBus.Abstractions;

namespace Nac.EventBus.Handlers;

/// <summary>
/// Scans assemblies for IEventHandler&lt;T&gt; implementations, registers them in DI,
/// and builds a frozen lookup mapping event type → set of handler types.
/// Supports fan-out: multiple handlers per event type.
///
/// Split into two phases so AddNacEventBus can be called multiple times
/// (each call registers handlers from its assemblies) while the
/// FrozenDictionary is built once lazily from the full accumulated set.
/// </summary>
internal static class EventHandlerRegistry
{
    private static readonly Type EventHandlerOpenType = typeof(IEventHandler<>);

    /// <summary>
    /// Phase 1 — called per AddNacEventBus call: scans assemblies and registers
    /// each IEventHandler&lt;T&gt; implementation as scoped in DI.
    /// Does NOT build the FrozenDictionary.
    /// </summary>
    internal static void RegisterHandlers(
        IServiceCollection services,
        IReadOnlyList<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var concreteTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false });

            foreach (var type in concreteTypes)
            {
                var handlerInterfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == EventHandlerOpenType);

                foreach (var iface in handlerInterfaces)
                {
                    services.AddScoped(iface, type);
                    services.AddScoped(type);
                }
            }
        }
    }

    /// <summary>
    /// Phase 2 — called once lazily by the DI factory: builds a frozen
    /// event-type → handler-type-set registry from the full accumulated
    /// assembly list. Uses DI to discover which concrete types are registered,
    /// so only handlers that survive TryAdd deduplication are included.
    /// </summary>
    internal static FrozenDictionary<Type, FrozenSet<Type>> BuildRegistry(
        IServiceProvider _,
        IReadOnlyList<Assembly> assemblies)
    {
        var registry = new Dictionary<Type, HashSet<Type>>();

        foreach (var assembly in assemblies)
        {
            var concreteTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false });

            foreach (var type in concreteTypes)
            {
                var handlerInterfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == EventHandlerOpenType);

                foreach (var iface in handlerInterfaces)
                {
                    var eventType = iface.GetGenericArguments()[0];
                    if (!registry.TryGetValue(eventType, out var handlers))
                    {
                        handlers = [];
                        registry[eventType] = handlers;
                    }
                    handlers.Add(type);
                }
            }
        }

        return registry.ToFrozenDictionary(
            kv => kv.Key,
            kv => kv.Value.ToFrozenSet());
    }
}
