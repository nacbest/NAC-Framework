using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nac.Abstractions.Messaging;
using Nac.Messaging.Extensions;

namespace Nac.Messaging.RabbitMQ.Extensions;

/// <summary>
/// Convenience extensions that register RabbitMQ as the <see cref="IEventBus"/>
/// implementation. Combines handler scanning, connection management,
/// publisher, and consumer worker in a single call.
/// </summary>
public static class RabbitMqMessagingExtensions
{
    /// <summary>
    /// Registers RabbitMQ as the distributed event bus for integration events.
    /// Publishes to a topic exchange and consumes via a background worker
    /// that dispatches to <see cref="IIntegrationEventHandler{TEvent}"/> implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Configure RabbitMQ connection and exchange settings.</param>
    /// <param name="handlerAssemblies">Assemblies to scan for integration event handlers.</param>
    public static IServiceCollection AddNacRabbitMQ(
        this IServiceCollection services,
        Action<RabbitMqOptions> configureOptions,
        params Assembly[] handlerAssemblies)
    {
        services.Configure(configureOptions);

        // Register dispatcher + handler scanning + EventTypeRegistry (from Nac.Messaging core)
        services.TryAddScoped<Nac.Messaging.Internal.IntegrationEventDispatcher>();
        services.AddNacIntegrationEventHandlers(handlerAssemblies);

        // Singleton connection manager — shared by publisher and consumer
        services.TryAddSingleton<RabbitMqConnectionManager>();

        // Publisher: singleton so all scopes share the same channel
        services.TryAddSingleton<RabbitMqEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<RabbitMqEventBus>());

        // Consumer background worker
        services.AddHostedService<RabbitMqConsumerWorker>();

        return services;
    }
}
