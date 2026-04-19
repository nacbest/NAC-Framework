using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nac.MultiTenancy.Resolution;
using Xunit;

namespace Nac.MultiTenancy.Tests.Resolution;

public class SubdomainTenantStrategyTests
{
    [Fact]
    public async Task ResolveAsync_WithValidSubdomain_ExtractsSubdomain()
    {
        // Arrange
        var strategy = new SubdomainTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tenant1.app.com");

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("tenant1");
    }

    [Fact]
    public async Task ResolveAsync_WithBaredomainNoDots_ReturnsNull()
    {
        // Arrange
        var strategy = new SubdomainTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost");

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithLocalhost_ReturnsNull()
    {
        // Arrange
        var strategy = new SubdomainTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost:3000");

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithIPAddress_ReturnsNull()
    {
        // Arrange
        var strategy = new SubdomainTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("192.168.1.1");

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithIPAddressAndPort_ReturnsNull()
    {
        // Arrange
        var strategy = new SubdomainTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("127.0.0.1:5000");

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithPortInHost_StripsPortBeforeExtraction()
    {
        // Arrange
        var strategy = new SubdomainTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tenant2.example.com:8080");

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("tenant2");
    }

    [Fact]
    public async Task ResolveAsync_WithMultipleLevelSubdomain_ExtractsFirstLevel()
    {
        // Arrange
        var strategy = new SubdomainTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("api.tenant3.example.com");

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("api");
    }
}
