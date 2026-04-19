using FluentAssertions;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Context;
using Xunit;

namespace Nac.MultiTenancy.Tests.Context;

public class TenantContextTests
{
    [Fact]
    public void SetCurrentTenant_WithValidTenant_SetsCurrent()
    {
        // Arrange
        ITenantContext context = new TenantContext();
        var tenant = new TenantInfo { Id = "tenant-1", Name = "Test Tenant" };

        // Act
        context.SetCurrentTenant(tenant);

        // Assert
        context.Current.Should().NotBeNull();
        context.Current!.Id.Should().Be("tenant-1");
        context.Current.Name.Should().Be("Test Tenant");
    }

    [Fact]
    public void TenantId_ReturnsCurrentTenantId()
    {
        // Arrange
        ITenantContext context = new TenantContext();
        var tenant = new TenantInfo { Id = "tenant-123", Name = "Test Tenant" };
        context.SetCurrentTenant(tenant);

        // Act
        var tenantId = context.TenantId;

        // Assert
        tenantId.Should().Be("tenant-123");
    }

    [Fact]
    public void SetCurrentTenant_WithNull_ClearsContext()
    {
        // Arrange
        ITenantContext context = new TenantContext();
        var tenant = new TenantInfo { Id = "tenant-1", Name = "Test Tenant" };
        context.SetCurrentTenant(tenant);

        // Act
        context.SetCurrentTenant(null);

        // Assert
        context.Current.Should().BeNull();
        context.TenantId.Should().BeNull();
    }

    [Fact]
    public void Current_WithoutSet_ReturnsNull()
    {
        // Arrange
        ITenantContext context = new TenantContext();

        // Act
        var current = context.Current;

        // Assert
        current.Should().BeNull();
    }

    [Fact]
    public void SetCurrentTenant_CanSwitchBetweenTenants()
    {
        // Arrange
        ITenantContext context = new TenantContext();
        var tenant1 = new TenantInfo { Id = "tenant-1", Name = "Tenant 1" };
        var tenant2 = new TenantInfo { Id = "tenant-2", Name = "Tenant 2" };

        // Act
        context.SetCurrentTenant(tenant1);
        var firstTenantId = context.TenantId;
        context.SetCurrentTenant(tenant2);
        var secondTenantId = context.TenantId;

        // Assert
        firstTenantId.Should().Be("tenant-1");
        secondTenantId.Should().Be("tenant-2");
    }
}
