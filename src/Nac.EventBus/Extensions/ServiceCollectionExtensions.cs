using System.Collections.Frozen;
using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Events;
using Nac.EventBus.Abstractions;
using Nac.EventBus.Handlers;
using Nac.EventBus.InMemory;
using Nac.EventBus.Outbox;
using Nac.Persistence.Outbox;

namespace Nac.EventBus.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers NAC event bus services.
    ///
    /// Safe to call multiple times (from different modules): handler registrations
    /// are accumulated across calls; the Channel, IEventPublisher, and
    /// InMemoryEventBusWorker are registered exactly once (first caller wins for
    /// Channel/Publisher; TryAddEnumerable ensures one Worker).
    ///
    /// FrozenDictionary (handler registry) is built lazily from a mutable
    /// NacEventBusAssemblyRegistry accumulator, so all assemblies registered
    /// across multiple AddNacEventBus calls are included.
    /// </summary>
    public static IServiceCollection AddNacEventBus(
        this IServiceCollection services,
        Action<NacEventBusOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new NacEventBusOptions();
        configure(options);

        // ── Accumulate assemblies across multiple AddNacEventBus calls ────────
        // NacEventBusAssemblyRegistry is a mutable list. First call registers it;
        // subsequent calls append to the same singleton instance.
        var registryAccumulator = GetOrAddAccumulator(services);
        foreach (var asm in options.Assemblies)
            registryAccumulator.Add(asm);

        // Register all IEventHandler<T> implementations from this call's assemblies.
        // AddScoped is idempotent for the same type: EF DI allows duplicate scoped
        // descriptors and resolves the last one, but EventDispatcher uses GetRequiredService
        // by concrete type — duplicates are harmless.
        EventHandlerRegistry.RegisterHandlers(services, options.Assemblies);

        // ── Outbox bridge (scoped; safe to re-register — last wins, same impl) ─
        services.AddScoped<IIntegrationEventPublisher, OutboxEventPublisher>();

        // ── Handler registry (FrozenDictionary) — built lazily from accumulator ─
        // TryAddSingleton: only the FIRST AddNacEventBus call registers this factory.
        // The factory closes over `registryAccumulator` which is the SAME mutable
        // list appended to by all subsequent calls. By the time DI resolves it
        // (after the app is built), all assemblies have been accumulated.
        services.TryAddSingleton(sp =>
            EventHandlerRegistry.BuildRegistry(
                sp,
                registryAccumulator.Assemblies));

        // ── Outbox type registry — same lazy pattern ───────────────────────────
        services.TryAddSingleton(sp =>
            new OutboxEventTypeRegistry(registryAccumulator.Assemblies));

        // Scoped — EventDispatcher receives IServiceProvider from its containing scope
        // so handler resolution is per-dispatch-scope, not root container.
        services.TryAddScoped<IEventDispatcher, EventDispatcher>();

        // ── InMemory transport — registered once (TryAdd) ─────────────────────
        if (options.UseInMemory)
        {
            var inMemoryOptions = new InMemoryEventBusOptions();
            var channelOptions = new BoundedChannelOptions(inMemoryOptions.ChannelCapacity)
            {
                FullMode    = inMemoryOptions.FullMode,
                SingleReader = true,
                SingleWriter = false,
            };

            // TryAddSingleton: only first caller's channel is kept.
            // All subsequent AddNacEventBus calls see the channel already registered.
            services.TryAddSingleton(_ => Channel.CreateBounded<IIntegrationEvent>(channelOptions));

            // Publisher resolves the singleton channel from DI — always consistent.
            services.TryAddSingleton<IEventPublisher>(sp =>
                new InMemoryEventBus(sp.GetRequiredService<Channel<IIntegrationEvent>>()));

            // AddHostedService already uses TryAddEnumerable internally, so only
            // the first registration takes effect. The worker resolves the channel
            // singleton from DI — guaranteed to match the publisher.
            services.AddHostedService(sp => new InMemoryEventBusWorker(
                sp.GetRequiredService<Channel<IIntegrationEvent>>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<InMemoryEventBusWorker>>()));
        }

        return services;
    }

    // ── Accumulator helpers ───────────────────────────────────────────────────

    private static NacEventBusAssemblyRegistry GetOrAddAccumulator(IServiceCollection services)
    {
        // Find existing accumulator descriptor (registered as singleton instance).
        var existing = services
            .FirstOrDefault(d => d.ServiceType == typeof(NacEventBusAssemblyRegistry));

        if (existing?.ImplementationInstance is NacEventBusAssemblyRegistry registry)
            return registry;

        var newRegistry = new NacEventBusAssemblyRegistry();
        services.AddSingleton(newRegistry);
        return newRegistry;
    }
}

/// <summary>
/// Mutable accumulator for assemblies registered across multiple AddNacEventBus calls.
/// Registered as a singleton so all callers share the same instance.
/// </summary>
internal sealed class NacEventBusAssemblyRegistry
{
    private readonly List<Assembly> _assemblies = [];

    internal IReadOnlyList<Assembly> Assemblies => _assemblies;

    internal void Add(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
            _assemblies.Add(assembly);
    }
}
