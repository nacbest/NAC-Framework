using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Extensions;
using Nac.WebApi.Extensions;
using Xunit;

namespace Nac.WebApi.Tests.Extensions;

/// <summary>
/// Regression tests for the AddNacWebApi() call-ordering guard.
/// Options must be configured BEFORE modules run, otherwise defaults would apply silently.
/// </summary>
public sealed class AddNacWebApiOrderingTests
{
    [Fact]
    public void AddNacWebApi_CalledAfterAddNacApplication_Throws()
    {
        // Arrange — AddNacApplication runs modules, which register defaults.
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        services.AddNacWebApi(); // first-time: fine, registers default options
        services.AddNacApplication<NacWebApiModule>(config);

        // Act — calling AddNacWebApi again AFTER AddNacApplication is misordering
        var act = () => services.AddNacWebApi(opts => opts.EnableApiVersioning = false);

        // Assert — must throw with actionable guidance
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*AddNacWebApi*BEFORE AddNacApplication*");
    }

    [Fact]
    public void AddNacWebApi_CalledBeforeAddNacApplication_Succeeds()
    {
        // Arrange & Act — correct order
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        var act = () =>
        {
            services.AddNacWebApi(opts => opts.EnableApiVersioning = false);
            services.AddNacApplication<NacWebApiModule>(config);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddNacWebApi_WithoutAddNacApplication_Succeeds()
    {
        // Arrange & Act — consumer may configure options before any application setup
        var services = new ServiceCollection();

        var act = () => services.AddNacWebApi();

        // Assert
        act.Should().NotThrow();
    }
}
