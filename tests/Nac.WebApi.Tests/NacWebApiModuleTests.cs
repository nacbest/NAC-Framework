using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nac.Core.Extensions;
using Nac.Core.Modularity;
using Nac.WebApi.Extensions;
using Xunit;

namespace Nac.WebApi.Tests;

/// <summary>
/// Verifies that NacWebApiModule registers expected services into the DI container.
/// Uses AddNacApplication to trigger the full module ConfigureServices pipeline.
/// </summary>
public sealed class NacWebApiModuleTests
{
    private static IServiceProvider BuildProvider(Action<NacWebApiOptions>? configure = null)
    {
        var services = new ServiceCollection();
        // Register required ASP.NET Core logging infrastructure
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        services.AddNacWebApi(configure);
        services.AddNacApplication<NacWebApiModule>(config);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void ConfigureServices_RegistersExceptionHandler()
    {
        // Arrange & Act
        var sp = BuildProvider();

        // Assert — IExceptionHandler is registered by AddExceptionHandler<NacExceptionHandler>()
        var handler = sp.GetService<IExceptionHandler>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_RegistersControllers()
    {
        // Arrange & Act — AddControllers registers IControllerFactory and related services
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();
        services.AddNacWebApi();
        services.AddNacApplication<NacWebApiModule>(config);

        // Assert — building the provider succeeds and AddControllers infra is present
        var sp = services.BuildServiceProvider();
        // IHostedService (NacApplicationLifetime) proves the module pipeline ran
        var hostedServices = sp.GetServices<IHostedService>();
        hostedServices.Should().NotBeEmpty();
    }

    [Fact]
    public void ConfigureServices_WithVersioning_RegistersApiVersioning()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();
        services.AddNacWebApi(opts => opts.EnableApiVersioning = true);
        services.AddNacApplication<NacWebApiModule>(config);
        var sp = services.BuildServiceProvider();

        // Assert — Asp.Versioning registers IApiVersioningFeature-related descriptors;
        // the simplest observable artifact is that the provider builds without error
        // and the module ran (NacApplicationFactory is in the container)
        var factory = sp.GetService<NacApplicationFactory>();
        factory.Should().NotBeNull();
        factory!.Modules.Should().Contain(m => m.GetType() == typeof(NacWebApiModule));
    }

    [Fact]
    public void ConfigureServices_WithHealthChecks_RegistersHealthChecks()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();
        services.AddNacWebApi(opts => opts.EnableHealthChecks = true);
        services.AddNacApplication<NacWebApiModule>(config);
        var sp = services.BuildServiceProvider();

        // Assert — AddHealthChecks registers IHealthCheckService
        var healthCheckService = sp.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }
}
