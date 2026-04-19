using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nac.Core.Extensions;
using Nac.Core.Modularity;
using Xunit;

namespace Nac.WebApi.Tests.Modularity;

public sealed class NacApplicationLifetimeTests
{
    public class LifecycleRootModule : NacModule
    {
        public static List<string> CallLog { get; } = [];

        public override void OnApplicationInitialization(ApplicationInitializationContext ctx)
            => CallLog.Add($"{GetType().Name}:Init");

        public override void OnApplicationShutdown(ApplicationShutdownContext ctx)
            => CallLog.Add($"{GetType().Name}:Shutdown");
    }

    [DependsOn(typeof(LifecycleRootModule))]
    public sealed class LifecycleChildModule : LifecycleRootModule;

    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddNacApplication<LifecycleChildModule>(config);
        return services.BuildServiceProvider();
    }

    public NacApplicationLifetimeTests()
    {
        LifecycleRootModule.CallLog.Clear();
    }

    [Fact]
    public async Task StartAsync_CallsOnApplicationInitializationInOrder()
    {
        // Arrange
        var sp = BuildProvider();
        var lifetime = sp.GetServices<IHostedService>().First();

        // Act
        await lifetime.StartAsync(CancellationToken.None);

        // Assert — root (dependency) initialises before child
        LifecycleRootModule.CallLog.Should().ContainInOrder(
            "LifecycleRootModule:Init",
            "LifecycleChildModule:Init");
    }

    [Fact]
    public async Task StopAsync_CallsOnApplicationShutdownInReverseOrder()
    {
        // Arrange
        var sp = BuildProvider();
        var lifetime = sp.GetServices<IHostedService>().First();
        await lifetime.StartAsync(CancellationToken.None);
        LifecycleRootModule.CallLog.Clear();

        // Act
        await lifetime.StopAsync(CancellationToken.None);

        // Assert — child shuts down before root (reverse of init order)
        LifecycleRootModule.CallLog.Should().ContainInOrder(
            "LifecycleChildModule:Shutdown",
            "LifecycleRootModule:Shutdown");
    }

    [Fact]
    public async Task Lifecycle_InitAndShutdownBothCalled()
    {
        // Arrange
        var sp = BuildProvider();
        var lifetime = sp.GetServices<IHostedService>().First();

        // Act
        await lifetime.StartAsync(CancellationToken.None);
        await lifetime.StopAsync(CancellationToken.None);

        // Assert — both phases recorded
        LifecycleRootModule.CallLog.Should().Contain(e => e.EndsWith(":Init"));
        LifecycleRootModule.CallLog.Should().Contain(e => e.EndsWith(":Shutdown"));
    }
}
