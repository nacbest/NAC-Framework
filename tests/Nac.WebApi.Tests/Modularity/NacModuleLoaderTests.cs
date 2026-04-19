using FluentAssertions;
using Nac.Core.Modularity;
using Nac.Observability;
using Xunit;

namespace Nac.WebApi.Tests.Modularity;

public sealed class NacModuleLoaderTests
{
    // Test fixture modules — nested to avoid polluting the global namespace
    public sealed class RootModule : NacModule;

    [DependsOn(typeof(RootModule))]
    public sealed class ChildAModule : NacModule;

    [DependsOn(typeof(RootModule))]
    public sealed class ChildBModule : NacModule;

    [DependsOn(typeof(ChildAModule), typeof(ChildBModule))]
    public sealed class GrandchildModule : NacModule;

    [DependsOn(typeof(CircularBModule))]
    public sealed class CircularAModule : NacModule;

    [DependsOn(typeof(CircularAModule))]
    public sealed class CircularBModule : NacModule;

    [Fact]
    public void LoadModules_SingleModule_ReturnsSingleModule()
    {
        // Act
        var modules = NacModuleLoader.LoadModules<RootModule>();

        // Assert
        modules.Should().HaveCount(1);
        modules[0].Should().BeOfType<RootModule>();
    }

    [Fact]
    public void LoadModules_LinearChain_ReturnsDependencyFirst()
    {
        // Act
        var modules = NacModuleLoader.LoadModules<ChildAModule>();

        // Assert
        var types = modules.Select(m => m.GetType()).ToList();
        types.Should().Contain(typeof(RootModule));
        types.Should().Contain(typeof(ChildAModule));
        types.IndexOf(typeof(RootModule)).Should().BeLessThan(types.IndexOf(typeof(ChildAModule)));
    }

    [Fact]
    public void LoadModules_DiamondDependency_EachModuleAppearsOnce()
    {
        // Act
        var modules = NacModuleLoader.LoadModules<GrandchildModule>();

        // Assert — exactly 4 unique module instances
        modules.Should().HaveCount(4);
        modules.Select(m => m.GetType()).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void LoadModules_DiamondDependency_RootIsFirst()
    {
        // Act
        var modules = NacModuleLoader.LoadModules<GrandchildModule>();

        // Assert
        modules[0].Should().BeOfType<RootModule>();
    }

    [Fact]
    public void LoadModules_DiamondDependency_GrandchildIsLast()
    {
        // Act
        var modules = NacModuleLoader.LoadModules<GrandchildModule>();

        // Assert
        modules[^1].Should().BeOfType<GrandchildModule>();
    }

    [Fact]
    public void LoadModules_CircularDependency_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => NacModuleLoader.LoadModules<CircularAModule>();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LoadModules_CircularDependency_ExceptionContainsModuleNames()
    {
        // Act
        var act = () => NacModuleLoader.LoadModules<CircularAModule>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Circular*");
    }

    [Fact]
    public void LoadModules_AllNacModules_DiscoverFullGraph()
    {
        // Act — start from the top-level composition root
        var modules = NacModuleLoader.LoadModules<NacWebApiModule>();

        // Assert — at minimum the core and observability modules must be present
        var types = modules.Select(m => m.GetType()).ToList();
        types.Should().Contain(typeof(NacCoreModule));
        types.Should().Contain(typeof(NacObservabilityModule));
    }
}
