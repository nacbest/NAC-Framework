using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Events;
using Nac.EventBus.Abstractions;
using Nac.EventBus.InMemory;
using Nac.EventBus.Tests.TestHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Nac.EventBus.Tests.InMemory;

public class InMemoryEventBusWorkerTests
{
    private static ILogger<InMemoryEventBusWorker> CreateLogger() =>
        Substitute.For<ILogger<InMemoryEventBusWorker>>();

    /// <summary>
    /// Builds a scope factory that returns a scope whose ServiceProvider resolves
    /// the given dispatcher. Uses a real ServiceCollection so AsyncScope works correctly.
    /// </summary>
    private static IServiceScopeFactory BuildScopeFactory(IEventDispatcher dispatcher)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task ExecuteAsync_ReadsEventAndDispatchesToDispatcher()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<IIntegrationEvent>();
        var dispatched = new TaskCompletionSource<IIntegrationEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        var dispatcher = Substitute.For<IEventDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                dispatched.TrySetResult(callInfo.Arg<IIntegrationEvent>());
                return Task.CompletedTask;
            });

        var worker = new InMemoryEventBusWorker(channel, BuildScopeFactory(dispatcher), CreateLogger());
        var cts = new CancellationTokenSource();

        var @event = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "dispatch-me");

        // Act
        var workerTask = worker.StartAsync(cts.Token);
        await channel.Writer.WriteAsync(@event);

        // Wait deterministically for dispatch to complete
        var received = await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Cleanup
        await cts.CancelAsync();

        // Assert
        received.Should().Be(@event);
        await dispatcher.Received(1).DispatchAsync(@event, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DispatcherThrows_WorkerContinuesWithNextEvent()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<IIntegrationEvent>();
        var secondDispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var callCount = 0;
        var dispatcher = Substitute.For<IEventDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("First dispatch failed");
                secondDispatched.TrySetResult();
                return Task.CompletedTask;
            });

        var worker = new InMemoryEventBusWorker(channel, BuildScopeFactory(dispatcher), CreateLogger());
        var cts = new CancellationTokenSource();

        var e1 = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "throws");
        var e2 = new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "succeeds");

        // Act
        var workerTask = worker.StartAsync(cts.Token);
        await channel.Writer.WriteAsync(e1);
        await channel.Writer.WriteAsync(e2);

        // Wait for second event to be dispatched
        await secondDispatched.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cts.CancelAsync();

        // Assert — worker survived the first failure and processed second event
        await dispatcher.Received(2).DispatchAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_StopsReadingLoop()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<IIntegrationEvent>();
        var dispatcher = Substitute.For<IEventDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var worker = new InMemoryEventBusWorker(channel, BuildScopeFactory(dispatcher), CreateLogger());
        var cts = new CancellationTokenSource();

        // Act — start worker, cancel immediately without writing any events
        var workerTask = worker.StartAsync(cts.Token);
        await cts.CancelAsync();

        // StopAsync gives the worker a chance to finish gracefully
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        // Assert — no events written, none dispatched
        await dispatcher.DidNotReceive().DispatchAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MultipleEvents_AllDispatched()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<IIntegrationEvent>();
        var expectedCount = 3;
        var sem = new SemaphoreSlim(0, expectedCount);

        var dispatcher = Substitute.For<IEventDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                sem.Release();
                return Task.CompletedTask;
            });

        var worker = new InMemoryEventBusWorker(channel, BuildScopeFactory(dispatcher), CreateLogger());
        var cts = new CancellationTokenSource();

        // Act
        var workerTask = worker.StartAsync(cts.Token);
        for (var i = 0; i < expectedCount; i++)
            await channel.Writer.WriteAsync(new SampleIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, $"evt-{i}"));

        // Wait for all three to be dispatched
        for (var i = 0; i < expectedCount; i++)
            await sem.WaitAsync(TimeSpan.FromSeconds(5));

        await cts.CancelAsync();

        // Assert
        await dispatcher.Received(expectedCount).DispatchAsync(
            Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
