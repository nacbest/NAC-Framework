using FluentAssertions;
using Nac.Core.Primitives;
using Xunit;

namespace Nac.Core.Tests.Primitives;

public class ValueObjectTests
{
    private sealed class TestValueObject : ValueObject
    {
        public string Name { get; }
        public int Value { get; }

        public TestValueObject(string name, int value)
        {
            Name = name;
            Value = value;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
        }
    }

    private sealed class ComplexValueObject : ValueObject
    {
        public string Name { get; }
        public int[] Numbers { get; }

        public ComplexValueObject(string name, int[] numbers)
        {
            Name = name;
            Numbers = numbers;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
            foreach (var number in Numbers)
            {
                yield return number;
            }
        }
    }

    [Fact]
    public void Equals_WithIdenticalComponents_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Test", 42);

        // Act & Assert
        vo1.Equals(vo2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentComponents_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Test", 43);

        // Act & Assert
        vo1.Equals(vo2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var vo = new TestValueObject("Test", 42);

        // Act & Assert
        vo.Equals((object)"string").Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var vo = new TestValueObject("Test", 42);

        // Act & Assert
        vo.Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void EqualsValueObject_WithNull_ReturnsFalse()
    {
        // Arrange
        var vo = new TestValueObject("Test", 42);

        // Act & Assert
        vo.Equals((ValueObject?)null).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_WithIdenticalComponents_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Test", 42);

        // Act & Assert
        (vo1 == vo2).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_WithDifferentComponents_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Test", 43);

        // Act & Assert
        (vo1 != vo2).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_WithIdenticalComponents_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Test", 42);

        // Act & Assert
        (vo1 != vo2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithIdenticalComponents_ReturnsSameValue()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Test", 42);

        // Act & Assert
        vo1.GetHashCode().Should().Be(vo2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentComponents_ReturnsDifferentValue()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Test", 43);

        // Act & Assert
        vo1.GetHashCode().Should().NotBe(vo2.GetHashCode());
    }

    [Fact]
    public void CanBeUsedInSet_WithIdenticalComponents_ContainsOneItem()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Test", 42);
        var set = new HashSet<TestValueObject> { vo1 };

        // Act
        set.Add(vo2);

        // Assert
        set.Should().HaveCount(1);
    }

    [Fact]
    public void CanBeUsedInDictionary()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Other", 100);
        var dict = new Dictionary<TestValueObject, string> { { vo1, "Value1" } };

        // Act
        dict.Add(vo2, "Value2");

        // Assert
        dict.Should().HaveCount(2);
        dict[vo1].Should().Be("Value1");
    }

    [Fact]
    public void ComplexValueObject_WithMultipleComponents_ComparesCorrectly()
    {
        // Arrange
        var vo1 = new ComplexValueObject("Test", [1, 2, 3]);
        var vo2 = new ComplexValueObject("Test", [1, 2, 3]);
        var vo3 = new ComplexValueObject("Test", [1, 2, 4]);

        // Act & Assert
        (vo1 == vo2).Should().BeTrue();
        (vo1 != vo3).Should().BeTrue();
    }

    [Fact]
    public void NullableComponent_InEqualityComponents_WorksCorrectly()
    {
        // Arrange
        var vo1 = new TestValueObject("", 0);
        var vo2 = new TestValueObject("", 0);

        // Act & Assert
        (vo1 == vo2).Should().BeTrue();
    }

    [Fact]
    public void ObjectEquals_WithIdenticalComponents_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("Test", 42);
        var vo2 = new TestValueObject("Test", 42);

        // Act & Assert
        vo1.Equals((object)vo2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Act & Assert
        ((TestValueObject?)null == (TestValueObject?)null).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        // Arrange
        var vo = new TestValueObject("Test", 42);

        // Act & Assert
        (vo == (TestValueObject?)null).Should().BeFalse();
    }
}
