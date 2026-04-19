using FluentAssertions;
using Nac.Core.Domain;
using Nac.Persistence.Specifications;
using Nac.Persistence.Tests.Helpers;
using Xunit;

namespace Nac.Persistence.Tests.Specifications;

public class SpecificationExtensionsTests
{
    [Fact]
    public void Where_WithSpecification_FiltersQueryable()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity { Name = "Target" },
            new TestEntity { Name = "Other" },
            new TestEntity { Name = "Target" }
        };
        var queryable = entities.AsQueryable();
        var spec = new NameSpec("Target");

        // Act
        var result = queryable.Where(spec).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Name.Should().Be("Target"));
    }

    [Fact]
    public void Where_WithEmptyResult_ReturnsEmpty()
    {
        // Arrange
        var entities = new[] { new TestEntity { Name = "Other" } };
        var queryable = entities.AsQueryable();
        var spec = new NameSpec("NonExistent");

        // Act
        var result = queryable.Where(spec).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Where_WithAndSpecification_AppliesCorrectFilter()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Target" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Target" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Other" }
        };
        var firstId = entities[0].Id;
        var queryable = entities.AsQueryable();

        var nameSpec = new NameSpec("Target");
        var idSpec = new IdSpec(firstId);
        var combined = nameSpec.And(idSpec);

        // Act
        var result = queryable.Where(combined).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(firstId);
        result[0].Name.Should().Be("Target");
    }

    [Fact]
    public void Where_WithOrSpecification_AppliesCorrectFilter()
    {
        // Arrange
        var firstId = Guid.NewGuid();
        var entities = new[]
        {
            new TestEntity { Id = firstId, Name = "One" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Other" }
        };
        var queryable = entities.AsQueryable();

        var nameSpec = new NameSpec("Target");
        var idSpec = new IdSpec(firstId);
        var combined = nameSpec.Or(idSpec);

        // Act
        var result = queryable.Where(combined).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(firstId);
    }

    [Fact]
    public void Where_WithNegatedSpecification_AppliesCorrectFilter()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity { Name = "Target" },
            new TestEntity { Name = "Other" },
            new TestEntity { Name = "Another" }
        };
        var queryable = entities.AsQueryable();

        var spec = new NameSpec("Target");
        var negated = spec.Not();

        // Act
        var result = queryable.Where(negated).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Name.Should().NotBe("Target"));
    }

    /// <summary>
    /// Specification filtering by name.
    /// </summary>
    private sealed class NameSpec : Specification<TestEntity>
    {
        private readonly string _name;

        public NameSpec(string name) => _name = name;

        public override System.Linq.Expressions.Expression<System.Func<TestEntity, bool>> ToExpression() =>
            e => e.Name == _name;
    }

    /// <summary>
    /// Specification filtering by ID.
    /// </summary>
    private sealed class IdSpec : Specification<TestEntity>
    {
        private readonly Guid _id;

        public IdSpec(Guid id) => _id = id;

        public override System.Linq.Expressions.Expression<System.Func<TestEntity, bool>> ToExpression() =>
            e => e.Id == _id;
    }
}
