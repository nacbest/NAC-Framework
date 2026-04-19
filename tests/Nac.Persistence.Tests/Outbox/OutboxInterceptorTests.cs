using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Nac.Core.Abstractions;
using Nac.Persistence.Interceptors;
using Nac.Persistence.Outbox;
using Nac.Persistence.Tests.Helpers;
using Xunit;

namespace Nac.Persistence.Tests.Outbox;

public class OutboxInterceptorTests
{
    private static TestDbContext CreateContextWithOutboxInterceptor(IDateTimeProvider dateTimeProvider)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new OutboxInterceptor(dateTimeProvider))
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task SavingChanges_AggregateWithIntegrationEvent_AddsOutboxEvent()
    {
        // Arrange
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        using var context = CreateContextWithOutboxInterceptor(dateTimeProvider);

        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.RaiseIntegrationEvent();

        // Act
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        // Assert
        var outboxEvents = await context.Set<OutboxEvent>().ToListAsync();
        outboxEvents.Should().HaveCount(1);
        outboxEvents[0].EventType.Should().Contain("TestIntegrationEvent");
        outboxEvents[0].Payload.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SavingChanges_AggregateWithDomainEventOnly_DoesNotCreateOutboxEvent()
    {
        // Arrange
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        using var context = CreateContextWithOutboxInterceptor(dateTimeProvider);

        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.RaiseSampleEvent(); // Domain-only event, not integration event

        // Act
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        // Assert
        var outboxEvents = await context.Set<OutboxEvent>().ToListAsync();
        outboxEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SavingChanges_OutboxEvent_HasCorrectPayload()
    {
        // Arrange
        var fixedTime = new DateTime(2026, 4, 16, 12, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(fixedTime);
        using var context = CreateContextWithOutboxInterceptor(dateTimeProvider);

        var aggregate = new TestAggregate(Guid.NewGuid(), "Test Aggregate");
        aggregate.RaiseIntegrationEvent();

        // Act
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        // Assert
        var outboxEvent = await context.Set<OutboxEvent>().FirstOrDefaultAsync();
        outboxEvent.Should().NotBeNull();
        outboxEvent!.EventType.Should().Contain("TestIntegrationEvent");
        outboxEvent.Payload.Should().NotBeEmpty();
        outboxEvent.CreatedAt.Should().Be(fixedTime);
        outboxEvent.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task SavingChanges_MultipleIntegrationEvents_CreatesMultipleOutboxEvents()
    {
        // Arrange
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        using var context = CreateContextWithOutboxInterceptor(dateTimeProvider);

        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.RaiseIntegrationEvent();
        aggregate.RaiseIntegrationEvent();

        // Act
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        // Assert
        var outboxEvents = await context.Set<OutboxEvent>().ToListAsync();
        outboxEvents.Should().HaveCount(2);
    }

    [Fact]
    public async Task SavingChanges_MixedEvents_CreatesOutboxOnlyForIntegrationEvents()
    {
        // Arrange
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        using var context = CreateContextWithOutboxInterceptor(dateTimeProvider);

        var aggregate = new TestAggregate(Guid.NewGuid(), "Test");
        aggregate.RaiseSampleEvent(); // Domain event only
        aggregate.RaiseIntegrationEvent(); // Integration event
        aggregate.RaiseSampleEvent(); // Domain event only

        // Act
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        // Assert
        var outboxEvents = await context.Set<OutboxEvent>().ToListAsync();
        outboxEvents.Should().HaveCount(1);
    }
}
