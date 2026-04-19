using FluentAssertions;
using Nac.Core.Domain;
using Xunit;

namespace Nac.Core.Tests.Domain;

public class GuardTests
{
    [Fact]
    public void NotNull_WithNonNullValue_ReturnsValue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guard.NotNull(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void NotNull_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        var action = () => Guard.NotNull(value, nameof(value));
        action.Should().Throw<ArgumentNullException>().WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrEmpty_WithValidString_ReturnsString()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guard.NotNullOrEmpty(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void NotNullOrEmpty_WithNullString_ThrowsArgumentException()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(value, nameof(value));
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var value = "";

        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(value, nameof(value));
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNullOrEmpty_WithWhitespaceOnly_ThrowsArgumentException()
    {
        // Arrange
        var value = "   ";

        // Act & Assert
        var action = () => Guard.NotNullOrEmpty(value, nameof(value));
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MaxLength_WithValidLength_ReturnsString()
    {
        // Arrange
        var value = "test";
        var maxLength = 10;

        // Act
        var result = Guard.MaxLength(value, maxLength, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void MaxLength_WithExactLength_ReturnsString()
    {
        // Arrange
        var value = "test";
        var maxLength = 4;

        // Act
        var result = Guard.MaxLength(value, maxLength, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void MaxLength_WithExceededLength_ThrowsArgumentException()
    {
        // Arrange
        var value = "test";
        var maxLength = 2;

        // Act & Assert
        var action = () => Guard.MaxLength(value, maxLength, nameof(value));
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotDefault_WithNonDefaultValue_ReturnsValue()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Guard.NotDefault(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void NotDefault_WithDefaultValue_ThrowsArgumentException()
    {
        // Arrange
        var value = 0;

        // Act & Assert
        var action = () => Guard.NotDefault(value, nameof(value));
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotDefault_WithDefaultGuid_ThrowsArgumentException()
    {
        // Arrange
        var value = Guid.Empty;

        // Act & Assert
        var action = () => Guard.NotDefault(value, nameof(value));
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNegative_WithPositiveValue_ReturnsValue()
    {
        // Arrange
        var value = 10m;

        // Act
        var result = Guard.NotNegative(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void NotNegative_WithZero_ReturnsZero()
    {
        // Arrange
        var value = 0m;

        // Act
        var result = Guard.NotNegative(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void NotNegative_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = -10m;

        // Act & Assert
        var action = () => Guard.NotNegative(value, nameof(value));
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Positive_WithPositiveValue_ReturnsValue()
    {
        // Arrange
        var value = 10m;

        // Act
        var result = Guard.Positive(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void Positive_WithZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = 0m;

        // Act & Assert
        var action = () => Guard.Positive(value, nameof(value));
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Positive_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = -10m;

        // Act & Assert
        var action = () => Guard.Positive(value, nameof(value));
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void InRange_WithValueInRange_ReturnsValue()
    {
        // Arrange
        var value = 5;
        var min = 1;
        var max = 10;

        // Act
        var result = Guard.InRange(value, min, max, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void InRange_WithMinBoundary_ReturnsValue()
    {
        // Arrange
        var value = 1;
        var min = 1;
        var max = 10;

        // Act
        var result = Guard.InRange(value, min, max, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void InRange_WithMaxBoundary_ReturnsValue()
    {
        // Arrange
        var value = 10;
        var min = 1;
        var max = 10;

        // Act
        var result = Guard.InRange(value, min, max, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void InRange_WithValueBelowMin_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = 0;
        var min = 1;
        var max = 10;

        // Act & Assert
        var action = () => Guard.InRange(value, min, max, nameof(value));
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void InRange_WithValueAboveMax_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = 11;
        var min = 1;
        var max = 10;

        // Act & Assert
        var action = () => Guard.InRange(value, min, max, nameof(value));
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
