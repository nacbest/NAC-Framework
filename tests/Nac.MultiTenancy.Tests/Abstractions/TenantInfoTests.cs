using FluentAssertions;
using Nac.MultiTenancy.Abstractions;
using Xunit;

namespace Nac.MultiTenancy.Tests.Abstractions;

public class TenantInfoTests
{
    [Fact]
    public void TenantInfo_RequiredPropertiesSet_CreatesInstance()
    {
        // Arrange & Act
        var tenant = new TenantInfo
        {
            Id = "tenant-1",
            Name = "Test Tenant"
        };

        // Assert
        tenant.Id.Should().Be("tenant-1");
        tenant.Name.Should().Be("Test Tenant");
    }

    [Fact]
    public void TenantInfo_ConnectionStringProperty_CanBeNull()
    {
        // Arrange & Act
        var tenant = new TenantInfo
        {
            Id = "tenant-1",
            Name = "Test Tenant",
            ConnectionString = null
        };

        // Assert
        tenant.ConnectionString.Should().BeNull();
    }

    [Fact]
    public void TenantInfo_IsActiveProperty_DefaultsTrue()
    {
        // Arrange & Act
        var tenant = new TenantInfo
        {
            Id = "tenant-1",
            Name = "Test Tenant"
        };

        // Assert
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TenantInfo_IsActiveProperty_CanBeSetFalse()
    {
        // Arrange & Act
        var tenant = new TenantInfo
        {
            Id = "tenant-1",
            Name = "Test Tenant",
            IsActive = false
        };

        // Assert
        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public void TenantInfo_PropertiesDictionary_InitializesEmpty()
    {
        // Arrange & Act
        var tenant = new TenantInfo
        {
            Id = "tenant-1",
            Name = "Test Tenant"
        };

        // Assert
        tenant.Properties.Should().BeEmpty();
    }

    [Fact]
    public void TenantInfo_PropertiesDictionary_CanBePopulated()
    {
        // Arrange
        var properties = new Dictionary<string, string?> { { "key1", "value1" } };

        // Act
        var tenant = new TenantInfo
        {
            Id = "tenant-1",
            Name = "Test Tenant",
            Properties = properties
        };

        // Assert
        tenant.Properties.Should().ContainKey("key1");
        tenant.Properties["key1"].Should().Be("value1");
    }

    [Fact]
    public void TenantInfo_AllProperties_SetAndRetrieved()
    {
        // Arrange & Act
        var tenant = new TenantInfo
        {
            Id = "tenant-123",
            Name = "Premium Tenant",
            ConnectionString = "Server=localhost;Database=tenant_123;",
            IsActive = true,
            Properties = new() { { "tier", "premium" }, { "region", "us-east-1" } }
        };

        // Assert
        tenant.Id.Should().Be("tenant-123");
        tenant.Name.Should().Be("Premium Tenant");
        tenant.ConnectionString.Should().Be("Server=localhost;Database=tenant_123;");
        tenant.IsActive.Should().BeTrue();
        tenant.Properties.Should().HaveCount(2);
        tenant.Properties["tier"].Should().Be("premium");
        tenant.Properties["region"].Should().Be("us-east-1");
    }
}
