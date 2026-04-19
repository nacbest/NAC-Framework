using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Nac.Core.Primitives;
using Nac.Persistence.Interceptors;
using Nac.Persistence.Tests.Helpers;
using Xunit;

namespace Nac.Persistence.Tests.Interceptors;

public class DomainEventInterceptorTests
{
    private static TestDbContext CreateContextWithDomainEventInterceptor(IServiceProvider serviceProvider)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DomainEventInterceptor(serviceProvider))
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task SavedChanges_AggregateWithEvents_DispatchesEvents()
    {
        // Arrange
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);
        var serviceProvider = services.BuildServiceProvider();

        using var context = CreateContextWithDomainEventInterceptor(serviceProvider);

        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.RaiseSampleEvent();
        aggregate.RaiseIntegrationEvent();

        // Act
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        // Assert
        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<List<IDomainEvent>>(events => events.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChanges_AggregateWithEvents_ClearsEventsAfterDispatch()
    {
        // Arrange
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);
        var serviceProvider = services.BuildServiceProvider();

        using var context = CreateContextWithDomainEventInterceptor(serviceProvider);

        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.RaiseSampleEvent();

        // Act
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SavedChanges_NoDispatcherRegistered_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        using var context = CreateContextWithDomainEventInterceptor(serviceProvider);

        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.RaiseSampleEvent();

        // Act & Assert
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SavedChanges_MultipleAggregates_DispatchesAllEvents()
    {
        // Arrange
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);
        var serviceProvider = services.BuildServiceProvider();

        using var context = CreateContextWithDomainEventInterceptor(serviceProvider);

        var aggregate1 = new TestAggregate(Guid.NewGuid(), "Agg1");
        aggregate1.RaiseSampleEvent();

        var aggregate2 = new TestAggregate(Guid.NewGuid(), "Agg2");
        aggregate2.RaiseIntegrationEvent();

        // Act
        context.Aggregates.AddRange(aggregate1, aggregate2);
        await context.SaveChangesAsync();

        // Assert
        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<List<IDomainEvent>>(events => events.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChanges_NoEvents_DoesNotCallDispatcher()
    {
        // Arrange
        var dispatcher = Substitute.For<IDomainEventDispatcher>();

        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);
        var serviceProvider = services.BuildServiceProvider();

        using var context = CreateContextWithDomainEventInterceptor(serviceProvider);

        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        // Don't raise any events

        // Act
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        // Assert
        await dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<List<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }
}
