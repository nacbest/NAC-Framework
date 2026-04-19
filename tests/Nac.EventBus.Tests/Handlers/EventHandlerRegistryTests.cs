using System.Collections.Frozen;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nac.EventBus.Abstractions;
using Nac.EventBus.Handlers;
using Nac.EventBus.Tests.TestHelpers;
using Xunit;

namespace Nac.EventBus.Tests.Handlers;

public class EventHandlerRegistryTests
{
    // EventHandlerRegistry is internal static — tested via split API:
    // RegisterHandlers (DI scan, per-call) + BuildRegistry (lazy, builds FrozenDictionary).
    // Accessible via InternalsVisibleTo.

    private static FrozenDictionary<Type, FrozenSet<Type>> RegisterAndBuild(
        IServiceCollection services, IReadOnlyList<Assembly> assemblies)
    {
        EventHandlerRegistry.RegisterHandlers(services, assemblies);
        return EventHandlerRegistry.BuildRegistry(services.BuildServiceProvider(), assemblies);
    }

    [Fact]
    public void RegisterHandlersAndBuildRegistry_FindsHandlerInAssembly()
    {
        var services = new ServiceCollection();
        var assemblies = new[] { typeof(SampleEventHandler).Assembly };

        var registry = RegisterAndBuild(services, assemblies);

        registry.Should().ContainKey(typeof(SampleIntegrationEvent));
    }

    [Fact]
    public void RegisterHandlersAndBuildRegistry_MultpleHandlersSameEvent_AllRegistered()
    {
        var services = new ServiceCollection();
        var assemblies = new[] { typeof(SampleEventHandler).Assembly };

        var registry = RegisterAndBuild(services, assemblies);

        registry.TryGetValue(typeof(SampleIntegrationEvent), out var handlerTypes).Should().BeTrue();
        handlerTypes!.Should().Contain(typeof(SampleEventHandler));
        handlerTypes.Should().Contain(typeof(SecondSampleEventHandler));
        handlerTypes.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void RegisterHandlersAndBuildRegistry_EmptyAssemblyList_ReturnsEmptyRegistry()
    {
        var services = new ServiceCollection();

        var registry = RegisterAndBuild(services, []);

        registry.Should().BeEmpty();
    }

    [Fact]
    public void RegisterHandlersAndBuildRegistry_AbstractClassesIgnored()
    {
        var services = new ServiceCollection();
        var assemblies = new[] { typeof(IEventHandler<>).Assembly };

        var act = () => RegisterAndBuild(services, assemblies);

        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterHandlersAndBuildRegistry_RegistryIsFrozenAfterBuild()
    {
        var services = new ServiceCollection();
        var assemblies = new[] { typeof(SampleEventHandler).Assembly };

        var registry = RegisterAndBuild(services, assemblies);

        registry.Should().BeAssignableTo<FrozenDictionary<Type, FrozenSet<Type>>>();
    }

    [Fact]
    public void RegisterHandlers_RegistersHandlersInDI()
    {
        var services = new ServiceCollection();
        var assemblies = new[] { typeof(SampleEventHandler).Assembly };

        EventHandlerRegistry.RegisterHandlers(services, assemblies);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IEventHandler<SampleIntegrationEvent>) &&
            d.ImplementationType == typeof(SampleEventHandler));
    }
}
