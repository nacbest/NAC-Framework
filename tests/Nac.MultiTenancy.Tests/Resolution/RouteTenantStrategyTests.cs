using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nac.MultiTenancy.Resolution;
using Xunit;

namespace Nac.MultiTenancy.Tests.Resolution;

public class RouteTenantStrategyTests
{
    [Fact]
    public async Task ResolveAsync_WithRouteValuePresent_ReturnsTenantId()
    {
        // Arrange
        var strategy = new RouteTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "tenant-1";

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("tenant-1");
    }

    [Fact]
    public async Task ResolveAsync_WithRouteValueMissing_ReturnsNull()
    {
        // Arrange
        var strategy = new RouteTenantStrategy();
        var context = new DefaultHttpContext();

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithEmptyRouteValue_ReturnsNull()
    {
        // Arrange
        var strategy = new RouteTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = string.Empty;

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithWhitespaceRouteValue_ReturnsNull()
    {
        // Arrange
        var strategy = new RouteTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "   ";

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithWhitespacePadding_TrimmedBeforeReturn()
    {
        // Arrange
        var strategy = new RouteTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "  tenant-456  ";

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("tenant-456");
    }
}
