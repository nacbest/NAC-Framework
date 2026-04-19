using FluentAssertions;
using Xunit;

namespace Nac.Caching.Tests;

public class CacheKeyTests
{
    [Fact]
    public void Create_WithTenant_PrependsTenantPrefix()
    {
        // Arrange
        const string tenantId = "tenant1";
        const string segment1 = "users";
        const string segment2 = "123";

        // Act
        var result = CacheKey.Create(tenantId, segment1, segment2);

        // Assert
        result.Should().Be("tenant1:users:123");
    }

    [Fact]
    public void Create_WithoutTenant_NoPrefix()
    {
        // Arrange
        const string segment1 = "users";
        const string segment2 = "123";

        // Act
        var result = CacheKey.Create(null, segment1, segment2);

        // Assert
        result.Should().Be("users:123");
    }

    [Fact]
    public void Create_SingleSegment_ReturnsSegment()
    {
        // Arrange
        const string key = "key";

        // Act
        var result = CacheKey.Create(null, key);

        // Assert
        result.Should().Be("key");
    }
}
