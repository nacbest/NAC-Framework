using FluentAssertions;
using Nac.Core.Domain;
using Xunit;

namespace Nac.Core.Tests.Domain;

public class SpecificationTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    private sealed class NameEqualsSpec(string name) : Specification<TestEntity>
    {
        public override System.Linq.Expressions.Expression<System.Func<TestEntity, bool>> ToExpression() =>
            e => e.Name == name;
    }

    private sealed class ValueGreaterThanSpec(int value) : Specification<TestEntity>
    {
        public override System.Linq.Expressions.Expression<System.Func<TestEntity, bool>> ToExpression() =>
            e => e.Value > value;
    }

    private sealed class ValueLessThanSpec(int value) : Specification<TestEntity>
    {
        public override System.Linq.Expressions.Expression<System.Func<TestEntity, bool>> ToExpression() =>
            e => e.Value < value;
    }

    [Fact]
    public void IsSatisfiedBy_WithMatchingEntity_ReturnsTrue()
    {
        // Arrange
        var spec = new NameEqualsSpec("Test");
        var entity = new TestEntity { Name = "Test" };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithNonMatchingEntity_ReturnsFalse()
    {
        // Arrange
        var spec = new NameEqualsSpec("Test");
        var entity = new TestEntity { Name = "Other" };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_CombinesTwoSpecifications()
    {
        // Arrange
        var nameSpec = new NameEqualsSpec("Test");
        var valueSpec = new ValueGreaterThanSpec(10);
        var combined = nameSpec.And(valueSpec);
        var entity = new TestEntity { Name = "Test", Value = 20 };

        // Act
        var result = combined.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void And_ReturnsFalseIfAnySpecFails()
    {
        // Arrange
        var nameSpec = new NameEqualsSpec("Test");
        var valueSpec = new ValueGreaterThanSpec(50);
        var combined = nameSpec.And(valueSpec);
        var entity = new TestEntity { Name = "Test", Value = 20 };

        // Act
        var result = combined.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Or_ReturnsTrueIfEitherSpecMatches()
    {
        // Arrange
        var nameSpec = new NameEqualsSpec("Test");
        var valueSpec = new ValueGreaterThanSpec(50);
        var combined = nameSpec.Or(valueSpec);
        var entity = new TestEntity { Name = "Test", Value = 20 };

        // Act
        var result = combined.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Or_ReturnsFalseIfBothSpecsFail()
    {
        // Arrange
        var nameSpec = new NameEqualsSpec("Test");
        var valueSpec = new ValueGreaterThanSpec(50);
        var combined = nameSpec.Or(valueSpec);
        var entity = new TestEntity { Name = "Other", Value = 20 };

        // Act
        var result = combined.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Not_InvertsSpecification()
    {
        // Arrange
        var nameSpec = new NameEqualsSpec("Test");
        var negated = nameSpec.Not();
        var entity = new TestEntity { Name = "Other" };

        // Act
        var result = negated.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Not_ReturnsFalseForMatchingSpec()
    {
        // Arrange
        var nameSpec = new NameEqualsSpec("Test");
        var negated = nameSpec.Not();
        var entity = new TestEntity { Name = "Test" };

        // Act
        var result = negated.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ChainedAnd_WithThreeSpecs()
    {
        // Arrange
        var spec1 = new NameEqualsSpec("Test");
        var spec2 = new ValueGreaterThanSpec(10);
        var spec3 = new ValueLessThanSpec(50);
        var combined = spec1.And(spec2).And(spec3);
        var entity = new TestEntity { Name = "Test", Value = 30 };

        // Act
        var result = combined.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ChainedOr_WithThreeSpecs()
    {
        // Arrange
        var spec1 = new NameEqualsSpec("Wrong");
        var spec2 = new ValueGreaterThanSpec(100);
        var spec3 = new ValueLessThanSpec(50);
        var combined = spec1.Or(spec2).Or(spec3);
        var entity = new TestEntity { Name = "Test", Value = 30 };

        // Act
        var result = combined.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ComplexCombination_AndOrNot()
    {
        // Arrange
        var nameSpec = new NameEqualsSpec("Test");
        var valueSpec = new ValueGreaterThanSpec(10);
        var negatedName = nameSpec.Not();
        var combined = negatedName.Or(valueSpec);
        var entity = new TestEntity { Name = "Other", Value = 5 };

        // Act
        var result = combined.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ToExpression_ReturnsValidLambda()
    {
        // Arrange
        var spec = new NameEqualsSpec("Test");

        // Act
        var expression = spec.ToExpression();

        // Assert
        expression.Should().NotBeNull();
        expression.Compile().Should().NotBeNull();
    }

    [Fact]
    public void And_CanBeUsedWithLinqQuery()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity { Name = "Test", Value = 20 },
            new TestEntity { Name = "Test", Value = 5 },
            new TestEntity { Name = "Other", Value = 30 }
        };
        var spec = new NameEqualsSpec("Test").And(new ValueGreaterThanSpec(10));
        var expression = spec.ToExpression();

        // Act
        var results = entities.Where(expression.Compile()).ToList();

        // Assert
        results.Should().HaveCount(1);
        results.First().Value.Should().Be(20);
    }
}
