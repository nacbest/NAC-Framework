using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Events;
using Nac.EventBus.Abstractions;

namespace Nac.EventBus.Handlers;

/// <summary>
/// Resolves and invokes all registered handlers for a given event type.
/// Per-handler errors are logged and swallowed — one failing handler does not block others.
/// Resolves each handler by concrete type to support fan-out (multiple handlers per event).
/// </summary>
internal sealed class EventDispatcher(
    FrozenDictionary<Type, FrozenSet<Type>> registry,
    IServiceProvider serviceProvider,
    ILogger<EventDispatcher> logger) : IEventDispatcher
{
    public async Task DispatchAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        var eventType = @event.GetType();
        if (!registry.TryGetValue(eventType, out var handlerTypes))
            return;

        var closedHandlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var method = closedHandlerType.GetMethod(nameof(IEventHandler<IIntegrationEvent>.HandleAsync))!;

        foreach (var handlerType in handlerTypes)
        {
            // Resolve by concrete type so each distinct handler is invoked (fan-out)
            var handler = serviceProvider.GetRequiredService(handlerType);
            try
            {
                var task = (Task)method.Invoke(handler, [@event, ct])!;
                await task;
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException!;
                if (inner is OperationCanceledException)
                    ExceptionDispatchInfo.Capture(inner).Throw();

                logger.LogError(inner, "Event handler {Handler} failed for {Event}.",
                    handler.GetType().Name, eventType.Name);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Event handler {Handler} failed for {Event}.",
                    handler.GetType().Name, eventType.Name);
            }
        }
    }
}
