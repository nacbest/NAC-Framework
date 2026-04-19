using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nac.MultiTenancy.Resolution;
using Xunit;

namespace Nac.MultiTenancy.Tests.Resolution;

public class ClaimTenantStrategyTests
{
    [Fact]
    public async Task ResolveAsync_WithValidClaim_ReturnsTenantId()
    {
        // Arrange
        var strategy = new ClaimTenantStrategy();
        var claims = new[] { new Claim(NacTenantClaims.TenantId, "tenant-1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("tenant-1");
    }

    [Fact]
    public async Task ResolveAsync_WithoutClaim_ReturnsNull()
    {
        // Arrange
        var strategy = new ClaimTenantStrategy();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "User1") }, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithoutUser_ReturnsNull()
    {
        // Arrange
        var strategy = new ClaimTenantStrategy();
        var context = new DefaultHttpContext();

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithEmptyClaimValue_ReturnsNull()
    {
        // Arrange
        var strategy = new ClaimTenantStrategy();
        var claims = new[] { new Claim(NacTenantClaims.TenantId, string.Empty) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithWhitespaceClaimValue_ReturnsNull()
    {
        // Arrange
        var strategy = new ClaimTenantStrategy();
        var claims = new[] { new Claim(NacTenantClaims.TenantId, "   ") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithWhitespacePadding_TrimmedBeforeReturn()
    {
        // Arrange
        var strategy = new ClaimTenantStrategy();
        var claims = new[] { new Claim(NacTenantClaims.TenantId, "  tenant-123  ") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        // Act
        var result = await strategy.ResolveAsync(context);

        // Assert
        result.Should().Be("tenant-123");
    }
}
