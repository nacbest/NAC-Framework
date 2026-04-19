using FluentAssertions;
using Nac.Core.Modularity;
using Nac.Identity;
using Nac.MultiTenancy;
using Nac.Observability;
using Nac.Persistence;
using Xunit;

namespace Nac.WebApi.Tests.Modularity;

/// <summary>
/// Verifies DependsOn graph declarations on the built-in NAC modules
/// using reflection — no instantiation required.
/// </summary>
public sealed class ModuleDependsOnTests
{
    private static Type[] GetDirectDeps(Type moduleType) =>
        moduleType
            .GetCustomAttributes(typeof(DependsOnAttribute), inherit: false)
            .Cast<DependsOnAttribute>()
            .SelectMany(a => a.DependedModuleTypes)
            .ToArray();

    [Fact]
    public void NacCoreModule_HasNoDependencies()
    {
        // Act
        var deps = GetDirectDeps(typeof(NacCoreModule));

        // Assert
        deps.Should().BeEmpty();
    }

    [Fact]
    public void NacWebApiModule_DependsOnCoreAndObservability()
    {
        // Act
        var deps = GetDirectDeps(typeof(NacWebApiModule));

        // Assert
        deps.Should().Contain(typeof(NacCoreModule));
        deps.Should().Contain(typeof(NacObservabilityModule));
    }

    [Fact]
    public void NacIdentityModule_DependsOnCoreAndPersistence()
    {
        // Act
        var deps = GetDirectDeps(typeof(NacIdentityModule));

        // Assert
        deps.Should().Contain(typeof(NacCoreModule));
        deps.Should().Contain(typeof(NacPersistenceModule));
    }

    [Fact]
    public void NacMultiTenancyModule_DependsOnCoreAndPersistence()
    {
        // Act
        var deps = GetDirectDeps(typeof(NacMultiTenancyModule));

        // Assert
        deps.Should().Contain(typeof(NacCoreModule));
        deps.Should().Contain(typeof(NacPersistenceModule));
    }

    [Fact]
    public void AllModules_DependOnAtLeastNacCoreModule()
    {
        // Arrange — collect all concrete NacModule subclasses reachable from NacWebApiModule
        var allModules = NacModuleLoader.LoadModules<NacWebApiModule>()
            .Select(m => m.GetType())
            .Where(t => t != typeof(NacCoreModule))
            .ToList();

        allModules.Should().NotBeEmpty();

        // Assert — every non-core module has NacCoreModule in its full transitive graph
        foreach (var moduleType in allModules)
        {
            var graph = NacModuleLoader.LoadModules(moduleType)
                .Select(m => m.GetType())
                .ToList();

            graph.Should().Contain(typeof(NacCoreModule),
                because: $"{moduleType.Name} must transitively depend on NacCoreModule");
        }
    }
}
