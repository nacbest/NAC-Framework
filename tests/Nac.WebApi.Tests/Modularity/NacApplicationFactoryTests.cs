using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Modularity;
using Xunit;

namespace Nac.WebApi.Tests.Modularity;

public sealed class NacApplicationFactoryTests
{
    // Tracking modules record which lifecycle phase was called and by whom
    public class TrackingRootModule : NacModule
    {
        public static List<string> CallLog { get; } = [];

        public override void PreConfigureServices(ServiceConfigurationContext ctx)
            => CallLog.Add($"{GetType().Name}:Pre");

        public override void ConfigureServices(ServiceConfigurationContext ctx)
            => CallLog.Add($"{GetType().Name}:Config");

        public override void PostConfigureServices(ServiceConfigurationContext ctx)
            => CallLog.Add($"{GetType().Name}:Post");
    }

    [DependsOn(typeof(TrackingRootModule))]
    public sealed class TrackingChildModule : TrackingRootModule;

    private static (IServiceCollection Services, IConfiguration Config) BuildContext()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        return (services, config);
    }

    public NacApplicationFactoryTests()
    {
        TrackingRootModule.CallLog.Clear();
    }

    [Fact]
    public void Create_ExecutesPreConfigureServicesOnAllModules()
    {
        // Arrange
        var (services, config) = BuildContext();

        // Act
        NacApplicationFactory.Create(typeof(TrackingChildModule), services, config);

        // Assert
        TrackingRootModule.CallLog.Should().Contain("TrackingRootModule:Pre");
        TrackingRootModule.CallLog.Should().Contain("TrackingChildModule:Pre");
    }

    [Fact]
    public void Create_ExecutesConfigureServicesOnAllModules()
    {
        // Arrange
        var (services, config) = BuildContext();

        // Act
        NacApplicationFactory.Create(typeof(TrackingChildModule), services, config);

        // Assert
        TrackingRootModule.CallLog.Should().Contain("TrackingRootModule:Config");
        TrackingRootModule.CallLog.Should().Contain("TrackingChildModule:Config");
    }

    [Fact]
    public void Create_ExecutesPostConfigureServicesOnAllModules()
    {
        // Arrange
        var (services, config) = BuildContext();

        // Act
        NacApplicationFactory.Create(typeof(TrackingChildModule), services, config);

        // Assert
        TrackingRootModule.CallLog.Should().Contain("TrackingRootModule:Post");
        TrackingRootModule.CallLog.Should().Contain("TrackingChildModule:Post");
    }

    [Fact]
    public void Create_ExecutesPhasesByPhase_NotByModule()
    {
        // Arrange — expected order: all Pre, then all Config, then all Post
        var (services, config) = BuildContext();
        var expected = new[]
        {
            "TrackingRootModule:Pre",
            "TrackingChildModule:Pre",
            "TrackingRootModule:Config",
            "TrackingChildModule:Config",
            "TrackingRootModule:Post",
            "TrackingChildModule:Post",
        };

        // Act
        NacApplicationFactory.Create(typeof(TrackingChildModule), services, config);

        // Assert
        TrackingRootModule.CallLog.Should().ContainInOrder(expected);
    }

    [Fact]
    public void Create_StoresModulesInFactory()
    {
        // Arrange
        var (services, config) = BuildContext();

        // Act
        var factory = NacApplicationFactory.Create(typeof(TrackingChildModule), services, config);

        // Assert
        factory.Modules.Should().HaveCount(2);
        factory.Modules.Select(m => m.GetType())
            .Should().Contain(typeof(TrackingRootModule))
            .And.Contain(typeof(TrackingChildModule));
    }
}
