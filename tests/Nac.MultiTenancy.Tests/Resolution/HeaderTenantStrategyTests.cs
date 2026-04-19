using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nac.MultiTenancy.Resolution;
using Xunit;

namespace Nac.MultiTenancy.Tests.Resolution;

public class HeaderTenantStrategyTests
{
    [Fact]
    public async Task ResolveAsync_WithHeaderPresent_ReturnsTenantId()
    {
        // Arrange
        var strategy = new HeaderTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Headers[NacTenantHeaders.TenantId] = "tenant-1";

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("tenant-1");
    }

    [Fact]
    public async Task ResolveAsync_WithHeaderMissing_ReturnsNull()
    {
        // Arrange
        var strategy = new HeaderTenantStrategy();
        var context = new DefaultHttpContext();

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithEmptyHeader_ReturnsNull()
    {
        // Arrange
        var strategy = new HeaderTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Headers[NacTenantHeaders.TenantId] = string.Empty;

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithWhitespaceHeader_ReturnsNull()
    {
        // Arrange
        var strategy = new HeaderTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Headers[NacTenantHeaders.TenantId] = "   ";

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithWhitespacePadding_TrimmedBeforeReturn()
    {
        // Arrange
        var strategy = new HeaderTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Headers[NacTenantHeaders.TenantId] = "  tenant-123  ";

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("tenant-123");
    }

    [Fact]
    public async Task ResolveAsync_WithMultipleHeaderValues_UsesFirst()
    {
        // Arrange
        var strategy = new HeaderTenantStrategy();
        var context = new DefaultHttpContext();
        context.Request.Headers[NacTenantHeaders.TenantId] = new[] { "tenant-1", "tenant-2" };

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("tenant-1");
    }
}
