using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nac.EventBus.Abstractions;
using Nac.EventBus.Extensions;
using Nac.EventBus.Tests.TestHelpers;
using Nac.Persistence.Outbox;
using Xunit;

namespace Nac.EventBus.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static IServiceProvider BuildProvider(bool useInMemory = true)
    {
        var services = new ServiceCollection();

        // Logging required by InMemoryEventBusWorker and EventDispatcher
        services.AddLogging();

        services.AddNacEventBus(opts =>
        {
            opts.RegisterHandlersFromAssembly(typeof(SampleEventHandler).Assembly);
            if (useInMemory) opts.UseInMemoryTransport();
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddNacEventBus_RegistersIEventPublisher()
    {
        // Arrange
        var sp = BuildProvider();

        // Act
        var publisher = sp.GetService<IEventPublisher>();

        // Assert
        publisher.Should().NotBeNull();
    }

    [Fact]
    public void AddNacEventBus_RegistersIEventDispatcher()
    {
        // Arrange
        var sp = BuildProvider();

        // Act — IEventDispatcher is scoped
        using var scope = sp.CreateScope();
        var dispatcher = scope.ServiceProvider.GetService<IEventDispatcher>();

        // Assert
        dispatcher.Should().NotBeNull();
    }

    [Fact]
    public void AddNacEventBus_RegistersIIntegrationEventPublisher_OutboxBridge()
    {
        // Arrange
        var sp = BuildProvider();

        // Act — OutboxEventPublisher is scoped
        using var scope = sp.CreateScope();
        var outboxPublisher = scope.ServiceProvider.GetService<IIntegrationEventPublisher>();

        // Assert
        outboxPublisher.Should().NotBeNull();
    }

    [Fact]
    public void AddNacEventBus_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddNacEventBus(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
