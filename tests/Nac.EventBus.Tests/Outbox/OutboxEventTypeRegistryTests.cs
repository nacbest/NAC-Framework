using FluentAssertions;
using Nac.EventBus.Outbox;
using Nac.EventBus.Tests.TestHelpers;
using Xunit;

namespace Nac.EventBus.Tests.Outbox;

public class OutboxEventTypeRegistryTests
{
    [Fact]
    public void Resolve_KnownTypeByAssemblyQualifiedName_ReturnsType()
    {
        // Arrange
        var registry = new OutboxEventTypeRegistry([typeof(SampleIntegrationEvent).Assembly]);
        var key = typeof(SampleIntegrationEvent).AssemblyQualifiedName!;

        // Act
        var result = registry.Resolve(key);

        // Assert
        result.Should().Be(typeof(SampleIntegrationEvent));
    }

    [Fact]
    public void Resolve_KnownTypeByFullName_ReturnsType()
    {
        // Arrange
        var registry = new OutboxEventTypeRegistry([typeof(SampleIntegrationEvent).Assembly]);
        var key = typeof(SampleIntegrationEvent).FullName!;

        // Act
        var result = registry.Resolve(key);

        // Assert
        result.Should().Be(typeof(SampleIntegrationEvent));
    }

    [Fact]
    public void Resolve_UnknownTypeName_ReturnsNull()
    {
        // Arrange
        var registry = new OutboxEventTypeRegistry([typeof(SampleIntegrationEvent).Assembly]);

        // Act
        var result = registry.Resolve("Nac.EventBus.Tests.NonExistent.GhostEvent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_EmptyAssemblyList_ReturnsNullForAnyType()
    {
        // Arrange
        var registry = new OutboxEventTypeRegistry([]);

        // Act
        var result = registry.Resolve(typeof(SampleIntegrationEvent).FullName!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_AbstractAndInterfaceTypesIgnored_NoThrow()
    {
        // Arrange — assembly contains IIntegrationEvent (interface) and abstract types
        var act = () => new OutboxEventTypeRegistry([typeof(Nac.Core.Abstractions.Events.IIntegrationEvent).Assembly]);

        // Assert — abstract/interface types skipped, no exception
        act.Should().NotThrow();
    }
}
