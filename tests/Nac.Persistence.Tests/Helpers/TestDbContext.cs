using Microsoft.EntityFrameworkCore;
using Nac.Persistence.Context;

namespace Nac.Persistence.Tests.Helpers;

/// <summary>
/// Concrete DbContext for testing. Includes DbSets for all test entity types
/// and applies configurations from the Nac.Persistence assembly to pick up
/// OutboxEvent configuration.
/// </summary>
public class TestDbContext : NacDbContext
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    public DbSet<TestAuditableEntity> AuditableEntities => Set<TestAuditableEntity>();
    public DbSet<TestSoftDeletableEntity> SoftDeletableEntities => Set<TestSoftDeletableEntity>();
    public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from Nac.Persistence assembly to pick up OutboxEvent configuration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NacDbContext).Assembly);
    }
}
