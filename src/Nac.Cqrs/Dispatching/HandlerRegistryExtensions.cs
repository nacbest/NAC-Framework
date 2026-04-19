using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nac.Cqrs.Commands;
using Nac.Cqrs.Queries;

namespace Nac.Cqrs.Dispatching;

/// <summary>
/// Scans assemblies for CQRS handler implementations, registers them in DI,
/// and builds the frozen handler registry used by <see cref="Sender"/>.
/// </summary>
internal static class HandlerRegistryExtensions
{
    private static readonly Type CommandHandlerOpenType = typeof(ICommandHandler<,>);
    private static readonly Type QueryHandlerOpenType = typeof(IQueryHandler<,>);

    /// <summary>
    /// Scans <paramref name="assemblies"/> for all concrete command and query handler types,
    /// registers each as scoped in <paramref name="services"/>, and returns a
    /// <see cref="FrozenDictionary{TKey,TValue}"/> mapping request type → handler wrapper.
    /// </summary>
    /// <param name="services">The service collection to register handlers into.</param>
    /// <param name="assemblies">Assemblies to scan for handler implementations.</param>
    /// <returns>
    /// A frozen dictionary keyed by request type for O(1) dispatch lookup.
    /// </returns>
    internal static FrozenDictionary<Type, RequestHandlerBase> RegisterHandlersAndBuildRegistry(
        IServiceCollection services,
        IReadOnlyList<Assembly> assemblies)
    {
        var registry = new Dictionary<Type, RequestHandlerBase>();

        foreach (var assembly in assemblies)
        {
            var concreteTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false });

            foreach (var type in concreteTypes)
            {
                RegisterCommandHandlers(services, registry, type);
                RegisterQueryHandlers(services, registry, type);
            }
        }

        return registry.ToFrozenDictionary();
    }

    // ── Command handlers ────────────────────────────────────────────────────

    private static void RegisterCommandHandlers(
        IServiceCollection services,
        Dictionary<Type, RequestHandlerBase> registry,
        Type handlerType)
    {
        var handlerInterfaces = handlerType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == CommandHandlerOpenType);

        foreach (var iface in handlerInterfaces)
        {
            var typeArgs = iface.GetGenericArguments(); // [TCommand, TResponse]
            var requestType = typeArgs[0];
            var responseType = typeArgs[1];

            // Register the concrete handler under the closed handler interface
            services.AddScoped(iface, handlerType);

            // Create a RequestHandlerWrapper<TCommand, TResponse> and add to registry
            AddToRegistry(registry, requestType, responseType, iface);
        }
    }

    // ── Query handlers ──────────────────────────────────────────────────────

    private static void RegisterQueryHandlers(
        IServiceCollection services,
        Dictionary<Type, RequestHandlerBase> registry,
        Type handlerType)
    {
        var handlerInterfaces = handlerType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == QueryHandlerOpenType);

        foreach (var iface in handlerInterfaces)
        {
            var typeArgs = iface.GetGenericArguments(); // [TQuery, TResponse]
            var requestType = typeArgs[0];
            var responseType = typeArgs[1];

            services.AddScoped(iface, handlerType);

            AddToRegistry(registry, requestType, responseType, iface);
        }
    }

    // ── Shared helper ───────────────────────────────────────────────────────

    private static void AddToRegistry(
        Dictionary<Type, RequestHandlerBase> registry,
        Type requestType,
        Type responseType,
        Type handlerServiceType)
    {
        if (registry.ContainsKey(requestType))
        {
            throw new InvalidOperationException(
                $"Duplicate handler registration for request type '{requestType.FullName}'. " +
                $"Attempted to register '{handlerServiceType.FullName}' but a handler is already registered.");
        }

        var wrapperType = typeof(RequestHandlerWrapper<,>)
            .MakeGenericType(requestType, responseType);

        // Pass the closed handler interface type so the wrapper can resolve it from DI.
        // Use non-public binding flag to invoke the internal constructor
        var wrapper = (RequestHandlerBase)Activator.CreateInstance(
            wrapperType,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [handlerServiceType],
            null)!;
        registry[requestType] = wrapper;
    }
}
