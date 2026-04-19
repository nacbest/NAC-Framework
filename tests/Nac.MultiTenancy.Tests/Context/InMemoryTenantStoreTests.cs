using FluentAssertions;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Context;
using Xunit;

namespace Nac.MultiTenancy.Tests.Context;

public class InMemoryTenantStoreTests
{
    [Fact]
    public async Task GetByIdAsync_WithSeededTenant_ReturnsTenant()
    {
        // Arrange
        var tenant1 = new TenantInfo { Id = "tenant-1", Name = "Tenant 1" };
        var tenant2 = new TenantInfo { Id = "tenant-2", Name = "Tenant 2" };
        var store = new InMemoryTenantStore(new[] { tenant1, tenant2 });

        // Act
        var result = await store.GetByIdAsync("tenant-1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("tenant-1");
        result.Name.Should().Be("Tenant 1");
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownTenantId_ReturnsNull()
    {
        // Arrange
        var tenant = new TenantInfo { Id = "tenant-1", Name = "Tenant 1" };
        var store = new InMemoryTenantStore(new[] { tenant });

        // Act
        var result = await store.GetByIdAsync("unknown-tenant");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithSeededTenants_ReturnsAll()
    {
        // Arrange
        var tenant1 = new TenantInfo { Id = "tenant-1", Name = "Tenant 1" };
        var tenant2 = new TenantInfo { Id = "tenant-2", Name = "Tenant 2" };
        var tenant3 = new TenantInfo { Id = "tenant-3", Name = "Tenant 3" };
        var store = new InMemoryTenantStore(new[] { tenant1, tenant2, tenant3 });

        // Act
        var result = await store.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.Id == "tenant-1");
        result.Should().Contain(t => t.Id == "tenant-2");
        result.Should().Contain(t => t.Id == "tenant-3");
    }

    [Fact]
    public async Task GetAllAsync_WithNoTenants_ReturnsEmpty()
    {
        // Arrange
        var store = new InMemoryTenantStore(new List<TenantInfo>());

        // Act
        var result = await store.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellation_CompletesAsync()
    {
        // Arrange
        var tenant = new TenantInfo { Id = "tenant-1", Name = "Tenant 1" };
        var store = new InMemoryTenantStore(new[] { tenant });
        var cts = new CancellationTokenSource();

        // Act
        var result = await store.GetByIdAsync("tenant-1", cts.Token);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("tenant-1");
    }

    [Fact]
    public async Task GetAllAsync_PreservesAllProperties()
    {
        // Arrange
        var tenant = new TenantInfo
        {
            Id = "tenant-1",
            Name = "Tenant 1",
            ConnectionString = "Server=localhost;",
            IsActive = false,
            Properties = new() { { "tier", "standard" } }
        };
        var store = new InMemoryTenantStore(new[] { tenant });

        // Act
        var result = await store.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        var retrieved = result[0];
        retrieved.Id.Should().Be("tenant-1");
        retrieved.Name.Should().Be("Tenant 1");
        retrieved.ConnectionString.Should().Be("Server=localhost;");
        retrieved.IsActive.Should().BeFalse();
        retrieved.Properties.Should().ContainKey("tier").WhoseValue.Should().Be("standard");
    }
}
