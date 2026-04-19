using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nac.Core.Domain;
using Nac.Core.Primitives;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.EfCore;
using Nac.Persistence.Context;
using Xunit;

namespace Nac.MultiTenancy.Tests.EfCore;

// Test entity implementing ITenantEntity and ISoftDeletable
internal class TestMultiTenantEntity : ITenantEntity, ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// Simple concrete ITenantContext implementation for testing
internal class SimpleTenantContext : ITenantContext
{
    private TenantInfo? _current;

    public TenantInfo? Current => _current;

    public void SetCurrentTenant(TenantInfo? tenant) => _current = tenant;
}

// Concrete test implementation of MultiTenantDbContext
internal class TestMultiTenantDbContext : MultiTenantDbContext
{
    public TestMultiTenantDbContext(DbContextOptions options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public DbSet<TestMultiTenantEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestMultiTenantEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<TestMultiTenantEntity>().Property(e => e.Name).IsRequired();
        modelBuilder.Entity<TestMultiTenantEntity>().Property(e => e.TenantId).IsRequired();
    }
}

public class MultiTenantDbContextTests
{
    [Fact]
    public void Constructor_InitializesWithTenantContext()
    {
        // Arrange
        var tenantContext = new SimpleTenantContext();
        var options = new DbContextOptionsBuilder<TestMultiTenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        var dbContext = new TestMultiTenantDbContext(options, tenantContext);

        // Assert
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCurrentTenantSet_ContextAvailable()
    {
        // Arrange
        var tenantContext = new SimpleTenantContext();
        var tenant = new TenantInfo { Id = "tenant-1", Name = "Tenant 1" };
        tenantContext.SetCurrentTenant(tenant);

        var options = new DbContextOptionsBuilder<TestMultiTenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        var dbContext = new TestMultiTenantDbContext(options, tenantContext);

        // Assert
        dbContext.Should().NotBeNull();
        tenantContext.Current.Should().NotBeNull();
        ((ITenantContext)tenantContext).TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void MultiTenantDbContext_InheritsFromNacDbContext()
    {
        // Arrange
        var tenantContext = new SimpleTenantContext();
        var options = new DbContextOptionsBuilder<TestMultiTenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        var dbContext = new TestMultiTenantDbContext(options, tenantContext);

        // Assert
        dbContext.Should().BeAssignableTo<NacDbContext>();
    }

    [Fact]
    public void OnModelCreating_CallsBase()
    {
        // Arrange
        var tenantContext = new SimpleTenantContext();
        var options = new DbContextOptionsBuilder<TestMultiTenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        var dbContext = new TestMultiTenantDbContext(options, tenantContext);
        dbContext.Database.EnsureCreated();

        // Assert — Database created successfully means OnModelCreating executed
        dbContext.Database.CanConnect().Should().BeTrue();
    }
}
