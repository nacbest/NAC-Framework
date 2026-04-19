using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Factory;
using Xunit;

namespace Nac.MultiTenancy.Tests.Factory;

public class TenantConnectionStringResolverTests
{
    [Fact]
    public void Resolve_WithTenantHasConnectionString_ReturnsTenantConnectionString()
    {
        // Arrange
        var tenant = new TenantInfo
        {
            Id = "tenant-1",
            Name = "Tenant 1",
            ConnectionString = "Server=tenant1.db;Database=tenant1;"
        };

        var tenantStore = Substitute.For<ITenantStore>();
        tenantStore.GetByIdAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenant));

        var configuration = Substitute.For<IConfiguration>();
        configuration.GetConnectionString("Default").Returns("Server=shared.db;Database=default;");

        var resolver = new TenantConnectionStringResolver(tenantStore, configuration);

        // Act
        var result = resolver.Resolve("tenant-1");

        // Assert
        result.Should().Be("Server=tenant1.db;Database=tenant1;");
    }

    [Fact]
    public void Resolve_WithTenantNoConnectionString_FallsBackToDefault()
    {
        // Arrange
        var tenant = new TenantInfo
        {
            Id = "tenant-2",
            Name = "Tenant 2",
            ConnectionString = null
        };

        var tenantStore = Substitute.For<ITenantStore>();
        tenantStore.GetByIdAsync("tenant-2", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenant));

        var configuration = Substitute.For<IConfiguration>();
        configuration.GetConnectionString("Default").Returns("Server=shared.db;Database=default;");

        var resolver = new TenantConnectionStringResolver(tenantStore, configuration);

        // Act
        var result = resolver.Resolve("tenant-2");

        // Assert
        result.Should().Be("Server=shared.db;Database=default;");
    }

    [Fact]
    public void Resolve_WithUnknownTenant_FallsBackToDefault()
    {
        // Arrange
        var tenantStore = Substitute.For<ITenantStore>();
        tenantStore.GetByIdAsync("unknown-tenant", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));

        var configuration = Substitute.For<IConfiguration>();
        configuration.GetConnectionString("Default").Returns("Server=shared.db;Database=default;");

        var resolver = new TenantConnectionStringResolver(tenantStore, configuration);

        // Act
        var result = resolver.Resolve("unknown-tenant");

        // Assert
        result.Should().Be("Server=shared.db;Database=default;");
    }

    [Fact]
    public void Constructor_WithoutDefaultConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantStore = Substitute.For<ITenantStore>();
        var configuration = Substitute.For<IConfiguration>();
        configuration.GetConnectionString("Default").Returns((string?)null);

        // Act
        var act = () => new TenantConnectionStringResolver(tenantStore, configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Default connection string not configured*");
    }

    [Fact]
    public void Resolve_WithMultipleTenants_ResolvesCorrectly()
    {
        // Arrange
        var tenant1 = new TenantInfo
        {
            Id = "tenant-1",
            Name = "Tenant 1",
            ConnectionString = "Server=tenant1.db;Database=tenant1;"
        };

        var tenant2 = new TenantInfo
        {
            Id = "tenant-2",
            Name = "Tenant 2",
            ConnectionString = "Server=tenant2.db;Database=tenant2;"
        };

        var tenantStore = Substitute.For<ITenantStore>();
        tenantStore.GetByIdAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenant1));
        tenantStore.GetByIdAsync("tenant-2", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenant2));

        var configuration = Substitute.For<IConfiguration>();
        configuration.GetConnectionString("Default").Returns("Server=shared.db;Database=default;");

        var resolver = new TenantConnectionStringResolver(tenantStore, configuration);

        // Act
        var result1 = resolver.Resolve("tenant-1");
        var result2 = resolver.Resolve("tenant-2");

        // Assert
        result1.Should().Be("Server=tenant1.db;Database=tenant1;");
        result2.Should().Be("Server=tenant2.db;Database=tenant2;");
    }
}
