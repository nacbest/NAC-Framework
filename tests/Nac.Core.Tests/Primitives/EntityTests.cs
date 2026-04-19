using FluentAssertions;
using Nac.Core.Primitives;
using Xunit;

namespace Nac.Core.Tests.Primitives;

public class EntityTests
{
    private sealed class TestEntity : Entity<int>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntity(int id, string name = "")
        {
            Id = id;
            Name = name;
        }
    }

    [Fact]
    public void Equals_WithSameId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(1, "Test");
        var entity2 = new TestEntity(1, "Different");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(1, "Test");
        var entity2 = new TestEntity(2, "Test");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(1);

        // Act
        var result = entity.Equals((TestEntity?)null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ObjectEquals_WithSameId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        // Act
        var result = entity1.Equals((object)entity2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ObjectEquals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(1);

        // Act
        var result = entity.Equals((object)"string");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_WithSameId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        // Act
        var result = entity1 == entity2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_WithDifferentId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);

        // Act
        var result = entity1 != entity2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_WithSameId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);

        // Act
        var result = entity1 != entity2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameId_ReturnsSameValue()
    {
        // Arrange
        var entity1 = new TestEntity(1, "Test");
        var entity2 = new TestEntity(1, "Different");

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentId_ReturnsDifferentValue()
    {
        // Arrange
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void CanBeUsedInSet_WithSameId_ContainsOneItem()
    {
        // Arrange
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(1);
        var set = new HashSet<TestEntity> { entity1 };

        // Act
        set.Add(entity2);

        // Assert
        set.Should().HaveCount(1);
    }

    [Fact]
    public void CanBeUsedInDictionary_WithIdAsKey()
    {
        // Arrange
        var entity1 = new TestEntity(1, "Entity 1");
        var entity2 = new TestEntity(2, "Entity 2");
        var dict = new Dictionary<TestEntity, string> { { entity1, "Value 1" } };

        // Act
        dict.Add(entity2, "Value 2");

        // Assert
        dict.Should().HaveCount(2);
        dict[entity1].Should().Be("Value 1");
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Act
        var result = (TestEntity?)null == (TestEntity?)null;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(1);

        // Act
        var result = entity == (TestEntity?)null;

        // Assert
        result.Should().BeFalse();
    }
}
