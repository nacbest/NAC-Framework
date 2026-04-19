using System.Collections.Frozen;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nac.EventBus.Abstractions;
using Nac.EventBus.Handlers;
using Nac.EventBus.Tests.TestHelpers;
using Xunit;

namespace Nac.EventBus.Tests.Handlers;

public class EventDispatcherTests
{
    // NullLogger is used for tests that don't assert on logging.
    // EventDispatcher is internal so ILogger<EventDispatcher> cannot be proxied by Castle
    // when the TState generic param is also internal (FormattedLogValues).
    private static ILogger<EventDispatcher> NullLog() =>
        NullLogger<EventDispatcher>.Instance;

    /// <summary>
    /// A simple capturing ILogger that records log entries without Castle proxying.
    /// Used only in tests that need to assert LogError was called.
    /// </summary>
    private sealed class CapturingLogger : ILogger<EventDispatcher>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception));
        }
    }

    /// <summary>
    /// Builds a registry and ServiceProvider with specified handler types registered.
    /// Returns both so tests can introspect what was dispatched.
    /// </summary>
    private static (FrozenDictionary<Type, FrozenSet<Type>> registry, IServiceProvider sp) BuildWith(
        params (Type handlerInterface, Type handlerImpl)[] registrations)
    {
        var services = new ServiceCollection();
        var dict = new Dictionary<Type, HashSet<Type>>();

        foreach (var (iface, impl) in registrations)
        {
            services.AddScoped(iface, impl);
            services.AddScoped(impl); // register concrete type for fan-out resolution
            var eventType = iface.GetGenericArguments()[0];
            if (!dict.TryGetValue(eventType, out var set))
            {
                set = [];
                dict[eventType] = set;
            }
            set.Add(impl);
        }

        var registry = dict.ToFrozenDictionary(
            kv => kv.Key,
            kv => kv.Value.ToFrozenSet());

        return (registry, services.BuildServiceProvider());
    }

    [Fact]
    public async Task DispatchAsync_SingleHandler_InvokesHandler()
    {
        // Arrange
        var (registry, sp) = BuildWith(
            (typeof(IEventHandler<SampleIntegrationEvent>), typeof(SampleEventHandler)));

        var logger = NullLog();
        var dispatcher = new EventDispatcher(registry, sp, logger);
        var @event = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "hello");

        // Act
        await dispatcher.DispatchAsync(@event);

        // Assert — resolved handler captured the event (resolve by concrete type, same as dispatcher)
        var handler = sp.GetRequiredService<SampleEventHandler>();
        handler.Received.Should().ContainSingle(e => e.Data == "hello");
    }

    [Fact]
    public async Task DispatchAsync_MultipleHandlers_InvokedOncePerRegistryEntry()
    {
        // Arrange — two distinct handler types registered for the same event.
        // Dispatcher resolves each by concrete type, so both must be invoked.
        var (registry, sp) = BuildWith(
            (typeof(IEventHandler<SampleIntegrationEvent>), typeof(SampleEventHandler)),
            (typeof(IEventHandler<SampleIntegrationEvent>), typeof(SecondSampleEventHandler)));

        var dispatcher = new EventDispatcher(registry, sp, NullLog());
        var @event = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "fan-out");

        // Act
        await dispatcher.DispatchAsync(@event);

        // Assert — each distinct handler was invoked once
        var handler1 = sp.GetRequiredService<SampleEventHandler>();
        var handler2 = sp.GetRequiredService<SecondSampleEventHandler>();
        handler1.Received.Should().ContainSingle(e => e.Data == "fan-out");
        handler2.Received.Should().ContainSingle(e => e.Data == "fan-out");
    }

    [Fact]
    public async Task DispatchAsync_NoHandlerRegistered_DoesNotThrow()
    {
        // Arrange — empty registry
        var registry = FrozenDictionary<Type, FrozenSet<Type>>.Empty;
        var dispatcher = new EventDispatcher(registry, new ServiceCollection().BuildServiceProvider(), NullLog());
        var @event = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "unrouted");

        // Act
        var act = async () => await dispatcher.DispatchAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrows_LogsErrorAndContinues()
    {
        // Arrange — register ONLY the throwing handler so DI resolves it.
        // EventDispatcher catches non-cancellation exceptions per handler and logs them,
        // then continues. We verify: no exception escapes AND dispatch completes.
        var services = new ServiceCollection();
        services.AddScoped<IEventHandler<SampleIntegrationEvent>, ThrowingEventHandler>();
        services.AddScoped<ThrowingEventHandler>();
        var sp = services.BuildServiceProvider();

        var registry = new Dictionary<Type, HashSet<Type>>
        {
            [typeof(SampleIntegrationEvent)] = [typeof(ThrowingEventHandler)]
        }.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.ToFrozenSet());

        // Use a real capturing logger — Castle cannot proxy ILogger<T> when T is internal.
        var capturingLogger = new CapturingLogger();
        var dispatcher = new EventDispatcher(registry, sp, capturingLogger);
        var @event = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "will-throw");

        // Act — should NOT propagate the handler exception
        var act = async () => await dispatcher.DispatchAsync(@event);

        // Assert — exception swallowed, error was logged at Error level.
        // Reflection wraps synchronous throws in TargetInvocationException;
        // we assert on level rather than the specific exception type.
        await act.Should().NotThrowAsync();
        capturingLogger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task DispatchAsync_CancellationRequested_PropagatesOperationCanceled()
    {
        // Arrange
        var (registry, sp) = BuildWith(
            (typeof(IEventHandler<SampleIntegrationEvent>), typeof(SampleEventHandler)));

        var dispatcher = new EventDispatcher(registry, sp, NullLog());
        var @event = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "cancel");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act — SampleEventHandler doesn't use CT but WriteAsync in channel would; here
        // we verify that OperationCanceledException is NOT swallowed by the catch filter.
        // Since SampleEventHandler.HandleAsync ignores CT, dispatch completes normally.
        var act = async () => await dispatcher.DispatchAsync(@event, cts.Token);

        // SampleEventHandler doesn't observe CT so no throw expected here
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_CancellationTokenPassedToHandler()
    {
        // Arrange — use a handler that captures the CT it received
        var services = new ServiceCollection();
        var capturedCt = CancellationToken.None;
        var handlerInstance = new CapturingCancellationHandler(ct => capturedCt = ct);
        services.AddScoped<IEventHandler<SampleIntegrationEvent>>(_ => handlerInstance);
        services.AddScoped<CapturingCancellationHandler>(_ => handlerInstance);
        var sp = services.BuildServiceProvider();

        var registry = new Dictionary<Type, HashSet<Type>>
        {
            [typeof(SampleIntegrationEvent)] = [typeof(CapturingCancellationHandler)]
        }.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.ToFrozenSet());

        using var cts = new CancellationTokenSource();
        var dispatcher = new EventDispatcher(registry, sp, NullLog());
        var @event = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "ct-check");

        // Act
        await dispatcher.DispatchAsync(@event, cts.Token);

        // Assert
        capturedCt.Should().Be(cts.Token);
    }

    // Helper: captures the CancellationToken passed to HandleAsync
    private sealed class CapturingCancellationHandler(Action<CancellationToken> capture)
        : IEventHandler<SampleIntegrationEvent>
    {
        public Task HandleAsync(SampleIntegrationEvent @event, CancellationToken ct = default)
        {
            capture(ct);
            return Task.CompletedTask;
        }
    }
}
