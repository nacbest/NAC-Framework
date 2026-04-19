using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nac.Core.Domain;
using Nac.Persistence.Repository;
using Nac.Persistence.Tests.Helpers;
using Xunit;

namespace Nac.Persistence.Tests.Repository;

public class RepositoryTests
{
    /// <summary>
    /// Creates an isolated InMemory DbContext for each test.
    /// </summary>
    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task AddAsync_InsertsEntity()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Name = "Test Entity" };

        // Act
        var result = await repository.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
        context.TestEntities.Should().Contain(entity);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        using var context = CreateContext();
        var entity = new TestEntity { Name = "Test Entity" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);

        // Act
        var result = await repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Entity");
        result.Id.Should().Be(entity.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ReturnsAllEntities()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Name = "Entity 1" },
            new TestEntity { Name = "Entity 2" },
            new TestEntity { Name = "Entity 3" }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);

        // Act
        var result = await repository.ListAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(e => e.Name == "Entity 1");
        result.Should().Contain(e => e.Name == "Entity 2");
        result.Should().Contain(e => e.Name == "Entity 3");
    }

    [Fact]
    public async Task ListAsync_WithSpecification_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Name = "Target" },
            new TestEntity { Name = "Other" },
            new TestEntity { Name = "Target" }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);
        var spec = new NameSpec("Target");

        // Act
        var result = await repository.ListAsync(spec);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Name.Should().Be("Target"));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSpec_ReturnsMatch()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Name = "Target" },
            new TestEntity { Name = "Other" }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);
        var spec = new NameSpec("Target");

        // Act
        var result = await repository.FirstOrDefaultAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Target");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithSpec_NoMatch_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[] { new TestEntity { Name = "Other" } };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);
        var spec = new NameSpec("NonExistent");

        // Act
        var result = await repository.FirstOrDefaultAsync(spec);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Name = "Entity 1" },
            new TestEntity { Name = "Entity 2" },
            new TestEntity { Name = "Entity 3" }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);

        // Act
        var count = await repository.CountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task CountAsync_WithSpecification_CountsMatching()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Name = "Target" },
            new TestEntity { Name = "Target" },
            new TestEntity { Name = "Other" }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);
        var spec = new NameSpec("Target");

        // Act
        var count = await repository.CountAsync(spec);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task AnyAsync_WithMatchingSpec_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[] { new TestEntity { Name = "Target" } };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);
        var spec = new NameSpec("Target");

        // Act
        var result = await repository.AnyAsync(spec);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_WithNonMatchingSpec_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[] { new TestEntity { Name = "Other" } };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);
        var spec = new NameSpec("NonExistent");

        // Act
        var result = await repository.AnyAsync(spec);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ModifiesEntity()
    {
        // Arrange
        using var context = CreateContext();
        var entity = new TestEntity { Name = "Original" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);
        entity.Name = "Updated";

        // Act
        await repository.UpdateAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repository.GetByIdAsync(entity.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        // Arrange
        using var context = CreateContext();
        var entity = new TestEntity { Name = "To Delete" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var repository = new Repository<TestEntity>(context);

        // Act
        await repository.DeleteAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repository.GetByIdAsync(entity.Id);
        retrieved.Should().BeNull();
    }

    /// <summary>
    /// Simple specification for testing - filters by name.
    /// </summary>
    private sealed class NameSpec : Specification<TestEntity>
    {
        private readonly string _name;

        public NameSpec(string name) => _name = name;

        public override System.Linq.Expressions.Expression<System.Func<TestEntity, bool>> ToExpression() =>
            e => e.Name == _name;
    }
}
