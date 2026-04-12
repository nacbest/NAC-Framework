using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Nac.Abstractions.Messaging;
using Nac.Messaging.InMemory;
using Nac.Messaging.Internal;
using Nac.Messaging.Outbox;
using Nac.Persistence;

namespace Nac.Messaging.Extensions;

/// <summary>
/// DI registration helpers for the NAC messaging layer.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory event bus with a background dispatch worker.
    /// Suitable for development and single-process deployments.
    /// </summary>
    public static IServiceCollection AddNacInMemoryMessaging(
        this IServiceCollection services,
        params Assembly[] handlerAssemblies)
    {
        RegisterCore(services, handlerAssemblies);

        services.TryAddSingleton<InMemoryEventBus>();
        services.TryAddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
        services.AddHostedService<InMemoryEventBusWorker>();

        return services;
    }

    /// <summary>
    /// Registers the outbox-based event bus for a module's <typeparamref name="TContext"/>.
    /// Events are written to the OutboxMessages table in the same transaction as business data
    /// and dispatched by a background worker.
    /// </summary>
    /// <remarks>
    /// In multi-module apps, the last call to this method determines which context backs
    /// <see cref="IEventBus"/>. For module-specific outbox control, inject
    /// <see cref="OutboxEventBus{TContext}"/> directly instead of <see cref="IEventBus"/>.
    /// </remarks>
    public static IServiceCollection AddNacOutboxMessaging<TContext>(
        this IServiceCollection services,
        params Assembly[] handlerAssemblies)
        where TContext : NacDbContext
    {
        RegisterCore(services, handlerAssemblies);

        // Not TryAdd: in multi-module apps the last registration wins for IEventBus.
        // Module-specific control available via OutboxEventBus<TContext> directly.
        services.AddScoped<OutboxEventBus<TContext>>();
        services.AddScoped<IEventBus>(sp => sp.GetRequiredService<OutboxEventBus<TContext>>());
        services.AddHostedService<OutboxWorker<TContext>>();

        return services;
    }

    /// <summary>
    /// Scans the given assemblies for <see cref="IIntegrationEventHandler{TEvent}"/>
    /// implementations and registers them in DI.
    /// </summary>
    public static IServiceCollection AddNacIntegrationEventHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var registry = GetOrCreateRegistry(services);

        foreach (var assembly in assemblies)
            ScanAndRegisterHandlers(services, assembly, registry);

        return services;
    }

    private static void RegisterCore(IServiceCollection services, Assembly[] handlerAssemblies)
    {
        services.TryAddScoped<IntegrationEventDispatcher>();

        var registry = GetOrCreateRegistry(services);

        foreach (var assembly in handlerAssemblies)
            ScanAndRegisterHandlers(services, assembly, registry);
    }

    private static EventTypeRegistry GetOrCreateRegistry(IServiceCollection services)
    {
        // Singleton registry — shared across the app lifetime
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(EventTypeRegistry));
        if (descriptor?.ImplementationInstance is EventTypeRegistry existing)
            return existing;

        var registry = new EventTypeRegistry();
        services.TryAddSingleton(registry);
        return registry;
    }

    private static void ScanAndRegisterHandlers(
        IServiceCollection services,
        Assembly assembly,
        EventTypeRegistry registry)
    {
        var handlerInterface = typeof(IIntegrationEventHandler<>);

        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != handlerInterface)
                    continue;

                var eventType = iface.GetGenericArguments()[0];
                registry.Register(eventType);
                services.AddTransient(iface, type);
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }
}
